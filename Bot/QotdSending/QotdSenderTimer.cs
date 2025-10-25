using OpenQotd.Bot.Database;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Bot.Exceptions;
using OpenQotd.Bot.Database.Entities;
using System.Collections.Concurrent;

namespace OpenQotd.Bot.QotdSending
{
    /// <summary>
    /// Periodic timer that checks which for configs need to be sent a QOTD and sends them.
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
        private static readonly PriorityQueue<CachedConfig, DateTime> _cache = new();
        /// <summary>
        /// Valid cached items by config ID.
        /// </summary>
        private static readonly Dictionary<int, CachedConfig> _cachedItems = [];

        /// <summary>
        /// Items to recache before the next cycle.
        /// </summary>
        public static readonly ConcurrentBag<int> ConfigIdsToRecache = [];
        /// <summary>
        /// Items to remove from cache before the next cycle.
        /// </summary>
        public static readonly ConcurrentBag<int> ConfigIdsToRemoveFromCache = [];

        /// <summary>
        /// Used to lock operations that directly modify <see cref="_cache"/> or <see cref="_cachedItems"/>, as those are not thread-safe.
        /// </summary>
        private static readonly SemaphoreSlim _cacheLock = new(1, 1);
        
        public struct ConfigNextSendInfo
        {
            public required int ConfigId;
            public required DateTime NextSendTime;
        }

        /// <summary>
        /// If the given config exists and is enabled for automatic QOTD sending, returns its next send time. Otherwise, returns null.
        /// </summary>
        public static async Task<ConfigNextSendInfo?> GetConfigNextSendInfo(int configId)
        {
            if (!_cachedItems.TryGetValue(configId, out CachedConfig? cachedConfig)) 
            {
                // Not found in cache, try to recache
                ConfigIdsToRecache.Add(configId);
                await RecacheOrRemoveRequiredElements();
                if (!_cachedItems.TryGetValue(configId, out cachedConfig))
                    return null; // Not found even after recache
            }

            return new ConfigNextSendInfo
            {
                ConfigId = cachedConfig.ConfigId,
                NextSendTime = cachedConfig.NextSendTime
            };
        }

        /// <summary>
        /// (Re-)loads all configs from the database and populates <see cref="_configIdsToSendQotdToday"/> and <see cref="_ignoredConfigIdsToday"/>.
        /// </summary>
        public static async Task LoadAllAsync()
        {
            await _cacheLock.WaitAsync();

            try
            {
                _cache.Clear();
                _cachedItems.Clear();

                HashSet<ConfigToSendElement> configsToSend = GetAllConfigsWithSendingEnabled();


            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private struct ConfigToSendElement
        {
            public required int ConfigId;
            public required DateTime? LastSentTimestamp;
            public required int Hour;
            public required int Minute;
            public required string? DayCondition;
            public required DateTime? DayConditionLastChanged;
        }
        private static HashSet<ConfigToSendElement> GetAllConfigsWithSendingEnabled()
        {
            using AppDbContext dbContext = new();

            return [.. dbContext.Configs
                    .Where(c => c.EnableAutomaticQotd)
                    .Select(c => new ConfigToSendElement { 
                        ConfigId = c.Id, 
                        LastSentTimestamp = c.LastSentTimestamp,
                        Hour = c.QotdTimeHourUtc, 
                        Minute = c.QotdTimeMinuteUtc, 
                        DayCondition = c.QotdTimeDayCondition, 
                        DayConditionLastChanged = c.QotdTimeDayConditionLastChangedTimestamp } )];
        }

        /// <summary>
        /// (Re-)caches or removes elements specified in <see cref="ConfigIdsToRecache"/>/<see cref="ConfigIdsToRemoveFromCache"/> from the cache.
        /// </summary>
        public static async Task RecacheOrRemoveRequiredElements()
        {
            if (ConfigIdsToRecache.IsEmpty && ConfigIdsToRemoveFromCache.IsEmpty)
                return; // Nothing to do

            await _cacheLock.WaitAsync();

            try
            {
                using AppDbContext dbContext = new();

                // Recache
                while (ConfigIdsToRecache.TryTake(out int configId))
                {
                    InvalideCachedItemIfExists(configId);

                    var config = await dbContext.Configs
                        .Where(c => c.Id == configId && c.EnableAutomaticQotd)
                        .Select(c => new
                        {
                            c.LastSentTimestamp,
                            c.QotdTimeHourUtc,
                            c.QotdTimeMinuteUtc,
                            c.QotdTimeDayCondition,
                            c.QotdTimeDayConditionLastChangedTimestamp
                        })
                        .FirstOrDefaultAsync();
                    if (config is null)
                        continue; // Config no longer exists or sending is disabled, do not recache

                    DateTime nextSendTime = QotdSenderTimeCalculations.GetNextSendTime(config.LastSentTimestamp, config.QotdTimeHourUtc, config.QotdTimeMinuteUtc, config.QotdTimeDayCondition, config.QotdTimeDayConditionLastChangedTimestamp);
                    CachedConfig newCachedConfig = new() { ConfigId = configId, NextSendTime = nextSendTime };
                    _cache.Enqueue(newCachedConfig, nextSendTime);
                    _cachedItems[configId] = newCachedConfig;
                }

                // Remove from cache
                while (ConfigIdsToRemoveFromCache.TryTake(out int configId))
                {
                    InvalideCachedItemIfExists(configId);
                }
            }
            finally
            {
                _cacheLock.Release();
            }


            static void InvalideCachedItemIfExists(int configId)
            {
                if (_cachedItems.TryGetValue(configId, out CachedConfig? cachedConfig))
                {
                    cachedConfig.IsValid = false;
                    _cachedItems.Remove(configId);
                }
            }
        }

        /// <summary>
        /// Send QOTDs to all configs that need one right now.
        /// </summary>
        public static async Task SendAvailableQotdsAsync()
        {
            await _cacheLock.WaitAsync();

            try
            {
                List<int> configsToSend = [];

                while (true)
                {
                    if (!_cache.TryPeek(out CachedConfig? peekedConfig, out DateTime peekedConfigTime))
                        break; // No more configs to process

                    if (!peekedConfig.IsValid)
                    {
                        // Remove invalidated config from cache
                        _ = _cache.Dequeue();
                        _ = _cachedItems.Remove(peekedConfig.ConfigId);
                        continue;
                    }

                    if (peekedConfigTime > DateTime.UtcNow)
                        break; // Next config is not ready yet

                    // Config is ready to be sent
                    // Also gets removed from cache. After sending, it will be re-cached if needed.
                    _ = _cache.Dequeue();
                    _ = _cachedItems.Remove(peekedConfig.ConfigId);
                    configsToSend.Add(peekedConfig.ConfigId);
                }

                await SendQotdsForConfigIdsAsync(configsToSend);
            }
            finally
            {
                _cacheLock.Release();
            }
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
                await SendNextQotdIgnoreExceptions(config, latestAvailableNotice);
            });

            await Console.Out.WriteLineAsync($"[{DateTime.UtcNow:O}] Sent {configs.Length}");
        }

        /// <summary>
        /// Send the next QOTD for the guild and catch and ignore/print all exceptions.
        /// </summary>
        private static async Task SendNextQotdIgnoreExceptions(Config config, Notices.Notice? latestAvailableNotice)
        {
            try
            {
                await QotdSender.FetchGuildAndSendNextQotdAsync(config, latestAvailableNotice);
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
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in FetchLoopAsync:\n{ex.Message}");
                }
            }
        }
    }
}
