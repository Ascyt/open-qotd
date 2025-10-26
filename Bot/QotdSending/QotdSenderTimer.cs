using OpenQotd.Database;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Exceptions;
using OpenQotd.Database.Entities;
using System.Collections.Concurrent;

namespace OpenQotd.QotdSending
{
    /// <summary>
    /// Periodic timer that checks which configs need to be sent a QOTD and sends them.
    /// </summary>
    public class QotdSenderTimer
    {
        private class CachedConfig
        {
            public required int ConfigId;
            public required DateTime NextSendTime;
            public bool IsValid = true;
        }

        /// <summary>
        /// Cached configs to send QOTDs for, ordered ascendingly by next send time.
        /// </summary>
        /// <remarks>
        /// PriorityQueue does not support removing arbitrary items, so instead we mark them as invalid when we want to remove them.
        /// </remarks>
        private static PriorityQueue<CachedConfig, DateTime> _cache = new();
        /// <summary>
        /// Valid cached items by config ID.
        /// </summary>
        private static ConcurrentDictionary<int, CachedConfig> _cachedItems = [];

        /// <summary>
        /// Items to recache before the next cycle.
        /// </summary>
        public static readonly ConcurrentBag<int> ConfigIdsToRecache = [];
        /// <summary>
        /// Items to remove from cache before the next cycle.
        /// </summary>
        public static readonly ConcurrentBag<int> ConfigIdsToRemoveFromCache = [];

        /// <summary>
        /// Used to lock operations that directly modify <see cref="_cache"/>, as it is not thread-safe.
        /// </summary>
        private static readonly Lock _cacheLock = new();

        /// <summary>
        /// If the given config exists and is enabled for automatic QOTD sending, returns its next send time. Otherwise, returns null.
        /// </summary>
        public static async Task<DateTime?> GetConfigNextSendTime(int configId)
        {
            if (!_cachedItems.TryGetValue(configId, out CachedConfig? cachedConfig)) 
            {
                ConfigToSendElement? config;
                using (AppDbContext dbContext = new())
                {
                    config = await dbContext.Configs
                        .Where(c => c.Id == configId && c.EnableAutomaticQotd)
                        .Select(c => new ConfigToSendElement
                        {
                            Id = c.Id,
                            LastSent = c.LastSentTimestamp,
                            Hour = c.QotdTimeHourUtc,
                            Minute = c.QotdTimeMinuteUtc,
                            DayCondition = c.QotdTimeDayCondition,
                            DayConditionLastChanged = c.QotdTimeDayConditionLastChangedTimestamp
                        })
                        .FirstOrDefaultAsync();

                    if (config is null)
                        return null; // Not found or not enabled
                }

                // Not found in cache, try to recache
                RecacheElement(config.Value);

                if (!_cachedItems.TryGetValue(configId, out cachedConfig))
                    return null; // Not found even after recache
            }

            return cachedConfig.NextSendTime;
        }

        /// <summary>
        /// (Re-)loads all configs from the database and populates <see cref="_cache"/> and <see cref="_cachedItems"/>.
        /// </summary>
        public static async Task LoadAllAsync()
        {
            HashSet<ConfigToSendElement> configs = await GetAllConfigsWithSendingEnabled();

            lock (_cacheLock) 
            { 
                _cachedItems = new ConcurrentDictionary<int, CachedConfig>(configs
                    .Select(c => new CachedConfig
                    {
                        ConfigId = c.Id,
                        NextSendTime = QotdSenderTimeCalculations.GetNextSendTime(c.LastSent, c.Hour, c.Minute, c.DayCondition, c.DayConditionLastChanged)
                    })
                    .Select(c => new KeyValuePair<int, CachedConfig>(c.ConfigId, c)));

                _cache.Clear();
                foreach (CachedConfig cached in _cachedItems.Values)
                {
                    _cache.Enqueue(cached, cached.NextSendTime);
                }
            }
        }

        private struct ConfigToSendElement
        {
            public required int Id;
            public required DateTime? LastSent;
            public required int Hour;
            public required int Minute;
            public required string? DayCondition;
            public required DateTime? DayConditionLastChanged;
        }
        private static async Task<HashSet<ConfigToSendElement>> GetAllConfigsWithSendingEnabled()
        {
            using AppDbContext dbContext = new();

            return await dbContext.Configs
                    .Where(c => c.EnableAutomaticQotd)
                    .Select(c => new ConfigToSendElement { 
                        Id = c.Id, 
                        LastSent = c.LastSentTimestamp,
                        Hour = c.QotdTimeHourUtc, 
                        Minute = c.QotdTimeMinuteUtc, 
                        DayCondition = c.QotdTimeDayCondition, 
                        DayConditionLastChanged = c.QotdTimeDayConditionLastChangedTimestamp } )
                    .ToHashSetAsync();
        }

        /// <summary>
        /// (Re-)caches or removes elements specified in <see cref="ConfigIdsToRecache"/>/<see cref="ConfigIdsToRemoveFromCache"/> from the cache.
        /// </summary>
        public static async Task RecacheOrRemoveRequiredElements()
        {
            if (ConfigIdsToRecache.IsEmpty && ConfigIdsToRemoveFromCache.IsEmpty)
                return; // Nothing to do

            HashSet<int> configIdsToRecache = [];

            using AppDbContext dbContext = new();

            // Recache
            while (ConfigIdsToRecache.TryTake(out int configId))
            {
                InvalidateCachedItemIfExists(configId);

                configIdsToRecache.Add(configId);
            }

            // Remove from cache
            while (ConfigIdsToRemoveFromCache.TryTake(out int configId))
            {
                InvalidateCachedItemIfExists(configId);
            }

            List<ConfigToSendElement> configsList = await dbContext.Configs
                .Where(c => configIdsToRecache.Contains(c.Id) && c.EnableAutomaticQotd)
                .Select(c => new ConfigToSendElement
                {
                    Id = c.Id,
                    LastSent = c.LastSentTimestamp,
                    Hour = c.QotdTimeHourUtc,
                    Minute = c.QotdTimeMinuteUtc,
                    DayCondition = c.QotdTimeDayCondition,
                    DayConditionLastChanged = c.QotdTimeDayConditionLastChangedTimestamp
                })
                .ToListAsync();

            foreach (ConfigToSendElement config in configsList)
            {
                RecacheElement(config);
            }

            static void InvalidateCachedItemIfExists(int configId)
            {
                if (_cachedItems.TryRemove(configId, out CachedConfig? cachedConfig))
                {
                    cachedConfig.IsValid = false;
                }
            }
        }

        private static void RecacheElement(ConfigToSendElement config)
        {
            DateTime nextSendTime = QotdSenderTimeCalculations.GetNextSendTime(config.LastSent, config.Hour, config.Minute, config.DayCondition, config.DayConditionLastChanged);
            CachedConfig newCachedConfig = new() { ConfigId = config.Id, NextSendTime = nextSendTime };
            lock (_cacheLock)
            {
                _cache.Enqueue(newCachedConfig, nextSendTime);
                _cachedItems[config.Id] = newCachedConfig;
            }
        }

        /// <summary>
        /// Send QOTDs to all configs that need one right now.
        /// </summary>
        public static async Task SendAvailableQotdsAsync()
        {
            List<int> configsToSend = [];

            lock (_cacheLock)
            {
                while (true)
                {
                    if (!_cache.TryPeek(out CachedConfig? peekedConfig, out DateTime peekedConfigTime))
                        break; // No more configs to process

                    if (!peekedConfig.IsValid)
                    {
                        // Remove invalidated config from cache
                        _ = _cache.Dequeue();
                        _ = _cachedItems.TryRemove(peekedConfig.ConfigId, out _); // no idea why there is no overload of TryRemove that doesn't return the removed item
                        continue;
                    }

                    if (peekedConfigTime > DateTime.UtcNow)
                        break; // Next config is not ready yet

                    // Config is ready to be sent
                    // Also gets removed from cache. After sending, it will be re-cached if needed.
                    _ = _cache.Dequeue();
                    _ = _cachedItems.TryRemove(peekedConfig.ConfigId, out _);
                    configsToSend.Add(peekedConfig.ConfigId);
                }
            }

            await SendQotdsForConfigIdsAsync(configsToSend);
        }

        /// <summary>
        /// Fetch the given configs, and send QOTDs, maximum <see cref="AppSettings.QotdSendingMaxDegreeOfParallelism"/> at a time.
        /// </summary>
        public static async Task SendQotdsForConfigIdsAsync(IEnumerable<int> configIds)
        {
            using AppDbContext dbContext = new();

            Config[] configs = await dbContext.Configs
                .Where(c => configIds.Contains(c.Id))
                .ToArrayAsync();

            await SendQotdsForConfigsAsync(configs);
        }

        /// <summary>
        /// Send QOTDs for the given configs, maximum <see cref="AppSettings.QotdSendingMaxDegreeOfParallelism"/> at a time.
        /// </summary>
        public static async Task SendQotdsForConfigsAsync(Config[] configs)
        {
            if (configs.Length == 0)
                return;

            Notices.Notice? latestAvailableNotice = Notices.GetLatestAvailableNotice();

            // Send QOTDs in parallel, but limit the degree of parallelism to avoid overwhelming the database or Discord API
            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = Program.AppSettings.QotdSendingMaxDegreeOfParallelism
            };
            await Parallel.ForEachAsync(configs, options, async (config, ct) =>
            {
                await SendNextQotdIgnoreExceptionsRecacheIfNecessary(config, latestAvailableNotice);
            });

            await Console.Out.WriteLineAsync($"[{DateTime.UtcNow:O}] Sent {configs.Length}");
        }

        /// <summary>
        /// Send the next QOTD for the guild and catch, ignore/print all exceptions and add it back to cache if necessary.
        /// </summary>
        private static async Task SendNextQotdIgnoreExceptionsRecacheIfNecessary(Config config, Notices.Notice? latestAvailableNotice)
        {
            bool shouldRecache = true;
            try
            {
                shouldRecache = await QotdSender.FetchGuildAndSendNextQotdAsync(config, latestAvailableNotice);
            }
            catch (QotdChannelNotFoundException)
            {
                // This exception is expected if the QOTD channel is not set for the guild.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending QOTD for config {config.Id} (guild {config.GuildId}): {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                if (shouldRecache)
                {
                    ConfigIdsToRecache.Add(config.Id);
                }
            }
        }

        /// <summary>
        /// Continuously filters by available QOTDs to send every <see cref="AppSettings.QotdSendingFetchLoopDelayMs"/> milliseconds.
        /// </summary>
        /// <remarks>
        /// Another check will not occur unless all QOTDs of the previous one have been sent.
        /// </remarks>
        public static async Task FetchLoopAsync(CancellationToken ct)
        {
            Console.WriteLine("Started fetch loop.");
            while (true)
            {
                try
                {
                    await Task.Delay(Program.AppSettings.QotdSendingFetchLoopDelayMs, ct);

                    await RecacheOrRemoveRequiredElements();
                    await SendAvailableQotdsAsync();
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Fetch loop cancelled.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in FetchLoopAsync:\n{ex.Message}");
                }
            }
        }
    }
}
