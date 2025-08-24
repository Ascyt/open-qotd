using CustomQotd.Bot.Helpers;
using CustomQotd.Database;
using CustomQotd.Database.Entities;
using CustomQotd.Features.Helpers;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace CustomQotd.Features.QotdSending
{
    public class QotdSender
    {
        private static readonly Random _random = new();

        public static async Task<Question?> GetRandomQotd(ulong guildId)
        {
            Question[] questions;
            using (var dbContext = new AppDbContext())
            {
                questions = await dbContext.Questions
                    .Where(q => q.GuildId == guildId && q.Type == QuestionType.Accepted)
                    .ToArrayAsync();
            }

            if (questions.Length == 0)
                return null;

            return questions[_random.Next(questions.Length)];
        }
        public static int GetRandomPreset(List<PresetSent> presetSents)
        {
            bool[] isPresetUsed = new bool[Presets.Values.Length];
            foreach (var presetSent in presetSents)
            {
                isPresetUsed[presetSent.PresetIndex] = true;
            }

            int index;
            int timeToLive = 0x1_000_000;
            do
            {
                index = _random.Next(Presets.Values.Length);
                timeToLive--;
            }
            while (isPresetUsed[index] && timeToLive > 0);

            if (timeToLive == 0)
            {
                throw new Exception("Time to live has expired (endless loop?)");
            }

            return index;
        }

        /// <exception cref="QotdSendException"></exception>
        public static async Task FetchGuildAndSendNextQotdAsync(ulong guildId, Notices.Notice? latestAvailableNotice)
        {
            DiscordGuild guild;
            try
            {
                guild = await Program.Client.GetGuildAsync(guildId);
            }
            catch (NotFoundException)
            {
                using var dbContext = new AppDbContext();
                Config? config = await dbContext.Configs.Where(c => c.GuildId == guildId).FirstOrDefaultAsync();

                if (config is null)
                    return;

                dbContext.Configs.Remove(config);

                await dbContext.SaveChangesAsync();
                Console.WriteLine($"Removed dead guild with ID {guildId}");
                return;
            }

            await SendNextQotdAsync(guild, latestAvailableNotice);
        }

        /// <exception cref="QotdSendException"></exception>
        public static async Task SendNextQotdAsync(DiscordGuild guild, Notices.Notice? latestAvaliableNotice)
        {
            await SendQotdAsync(guild, await GetRandomQotd(guild.Id), latestAvaliableNotice);
        }

        private struct SendQotdData
        {
            public Config config;
            public DiscordGuild guild;

            public DateTime? previousLastSentTimestamp;
            public Notices.Notice? latestAvailableNotice;

            private DiscordChannel? _qotdChannel;

            /// <exception cref="QotdChannelNotFoundException"></exception>
            public async Task<DiscordChannel> GetQotdChannelAsync()
            {
                if (_qotdChannel is not null)
                    return _qotdChannel;

                try 
                { 
                    _qotdChannel = await guild.GetChannelAsync(config.QotdChannelId);
                }
                catch (NotFoundException)
                {
                    throw new QotdChannelNotFoundException();
                }
                return _qotdChannel;
            }
        }
        public abstract class QotdSendException : Exception
        {
            public QotdSendException() : base() { }
            public QotdSendException(string message) : base(message) { }
            public QotdSendException(string message, Exception innerException) : base(message, innerException) { }
        }
        public class QotdChannelNotFoundException : QotdSendException
        {
            public QotdChannelNotFoundException() : base() { }
        }

        public static async Task SendQotdAsync(DiscordGuild guild, Question? question, Notices.Notice? latestAvailableNotice)
        {
            // Fetch the config and update the last sent timestamp
            Config? config;
            DateTime? previousLastSentTimestamp;
            using (var dbContext = new AppDbContext())
            {
                config = await dbContext.Configs
                    .Where(c => c.GuildId == guild.Id)
                    .FirstOrDefaultAsync();

                if (config == null)
                    return;

                previousLastSentTimestamp = config.LastSentTimestamp;
                config.LastSentTimestamp = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();
            }

            SendQotdData sendQotdData = new()
            {
                config = config,
                guild = guild,
                previousLastSentTimestamp = previousLastSentTimestamp,
                latestAvailableNotice = latestAvailableNotice
            };

            if (question == null)
            {
                if (config.EnableQotdAutomaticPresets)
                {
                    List<PresetSent> presetSents;
                    using (var dbContext = new AppDbContext())
                    {
                        presetSents = (await dbContext.PresetSents
                            .Where(ps => ps.GuildId == guild.Id)
                            .ToListAsync());
                    }

                    if (presetSents.Count < Presets.Values.Length)
                    {
                        await SendQotdPresetAsync(sendQotdData, presetSents);
                        return;
                    }
                }

                await SendQotdUnavailableMessageIfEnabledAsync(sendQotdData);

                return;
            }


            await SendQotdQuestionAsync(sendQotdData, question);
        }
        private static void AddSuggestButtonIfEnabled(Config config, DiscordMessageBuilder messageBuilder)
        {
            if (!config.EnableSuggestions)
                return;

            DiscordButtonComponent suggestButton = new(DiscordButtonStyle.Secondary, "suggest-qotd", "Suggest a new QOTD");

            messageBuilder.AddActionRowComponent(suggestButton);
        }
        private static async Task SendQotdUnavailableMessageIfEnabledAsync(SendQotdData d)
        {
            if (!d.config.EnableQotdUnavailableMessage)
                return;

            DiscordChannel qotdChannel = await d.GetQotdChannelAsync();

            DiscordMessageBuilder messageBuilder = new();

            messageBuilder.AddEmbed(
                MessageHelpers.GenericEmbed(title: "No QOTD Available", message: $"There is currently no Question Of The Day available." +
                (d.config.EnableSuggestions ? $"\n\n*Suggest some using `/qotd`!*" : ""), color: "#dc5051"));
            AddSuggestButtonIfEnabled(d.config, messageBuilder);

            await qotdChannel.SendMessageAsync(messageBuilder);
            await SendNoticeIfAvailable(d);
        }
        /// <returns>Whether or not sending was successful</returns>
        private static async Task SendQotdPresetAsync(SendQotdData d, List<PresetSent> presetSents)
        {
            DiscordChannel qotdChannel = await d.GetQotdChannelAsync();

            int presetIndex = GetRandomPreset(presetSents);
            int presetsAvailable = Presets.Values.Length - presetSents.Count;

            DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbed(
                MessageHelpers.GenericEmbed($"Question Of The Day",
                $"**{Presets.Values[presetIndex]}**\n" +
                $"\n" +
                $"*Preset Question*",
                color: "#8acfac")
                .WithFooter($"{presetsAvailable - 1} preset{((presetsAvailable - 1) == 1 ? "" : "s")} left{(d.config.EnableSuggestions ? $", /qotd to suggest" : "")} \x2022 Preset ID: {presetIndex}")
                );
            await AddPingRoleIfEnabledAndExistent(d, messageBuilder);

            AddSuggestButtonIfEnabled(d.config, messageBuilder);

            DiscordMessage sentMessage = await qotdChannel.SendMessageAsync(messageBuilder);

            using (var dbContext = new AppDbContext())
            {
                Config? foundConfig = await dbContext.Configs.Where(c => c.GuildId == d.guild.Id).FirstOrDefaultAsync();
                if (foundConfig != null)
                {
                    foundConfig.LastQotdMessageId = sentMessage.Id;
                }

                await dbContext.PresetSents.AddAsync(new PresetSent() { GuildId = d.guild.Id, PresetIndex = presetIndex });

                await dbContext.SaveChangesAsync();
            }

            await SendNoticeIfAvailable(d);
            await PinMessageIfEnabled(d, sentMessage);
            await CreateThreadIfEnabled(d, sentMessage, null);
        }
        private static async Task SendQotdQuestionAsync(SendQotdData d, Question question)
        {
            DiscordMessageBuilder messageBuilder = new();

            await AddPingRoleIfEnabledAndExistent(d, messageBuilder);

            int acceptedQuestionsCount;
            int sentQuestionsCount;
            using (var dbContext = new AppDbContext())
            {
                acceptedQuestionsCount = await dbContext.Questions.Where(q => q.GuildId == d.guild.Id && q.Type == QuestionType.Accepted).CountAsync()
                    - 1;
                sentQuestionsCount = await dbContext.Questions.Where(q => q.GuildId == d.guild.Id && q.Type == QuestionType.Sent).CountAsync()
                    + 1;
            }

            messageBuilder.AddEmbed(
                MessageHelpers.GenericEmbed($"Question Of The Day #{sentQuestionsCount}",
                $"**{question.Text}**\n" +
                $"\n" +
                $"*Submitted by <@{question.SubmittedByUserId}>*",
                color: "#8acfac")
                .WithFooter($"{acceptedQuestionsCount} question{(acceptedQuestionsCount == 1 ? "" : "s")} left{(d.config.EnableSuggestions ? $", /qotd to suggest" : "")} \x2022 Question ID: {question.GuildDependentId}")
                );

            DiscordChannel qotdChannel = await d.GetQotdChannelAsync();

            DiscordMessage sentMessage = await qotdChannel.SendMessageAsync(messageBuilder);
            using (var dbContext = new AppDbContext())
            {
                Config? foundConfig = await dbContext.Configs.Where(c => c.GuildId == d.guild.Id).FirstOrDefaultAsync();
                if (foundConfig != null)
                {
                    foundConfig.LastQotdMessageId = sentMessage.Id;
                }

                Question? foundQuestion = await dbContext.Questions.Where(q => q.Id == question.Id).FirstOrDefaultAsync();

                if (foundQuestion != null)
                {
                    foundQuestion.Type = QuestionType.Sent;
                    foundQuestion.SentNumber = sentQuestionsCount + 1;
                    foundQuestion.SentTimestamp = DateTime.UtcNow;
                }

                await dbContext.SaveChangesAsync();
            }

            if (acceptedQuestionsCount == 0)
            {
                DiscordMessageBuilder lastQuestionWarning = new();

                int presetsSent;
                using (var dbContext = new AppDbContext())
                {
                    presetsSent = await dbContext.PresetSents.Where(ps => ps.GuildId == d.guild.Id).CountAsync();
                }
                int presetsLeft = Presets.Values.Length - presetsSent;

                lastQuestionWarning.AddEmbed(
                    MessageHelpers.GenericWarningEmbed(title: "Warning: Last QOTD", message:
                    "There is no more Accepted QOTD of this server left." +
                    (presetsLeft > 0 && d.config.EnableQotdAutomaticPresets ? $"\nIf none are added, one of **{presetsLeft} Presets** will start to be used instead." : "") +
                    (d.config.EnableSuggestions ? $"\n\n*More can be suggested using `/qotd`!*" : "")));

                await qotdChannel.SendMessageAsync(lastQuestionWarning);
            }

            await SendNoticeIfAvailable(d);

            await PinMessageIfEnabled(d, sentMessage);
            await CreateThreadIfEnabled(d, sentMessage, sentQuestionsCount);
        }

        private static async Task SendNoticeIfAvailable(SendQotdData d)
        {
            if (d.latestAvailableNotice is null)
                return;

            if (d.config.NoticesLevel == Config.NoticeLevel.None)
                return;

            if (d.config.NoticesLevel == Config.NoticeLevel.Important && !d.latestAvailableNotice.IsImportant)
                return;

            if ((d.previousLastSentTimestamp is null && (d.latestAvailableNotice.Date >= DateTime.UtcNow.AddDays(-2))) || // not sent a qotd yet? send if notice is less than 2 days old
                (d.previousLastSentTimestamp is not null && (d.latestAvailableNotice.Date > d.previousLastSentTimestamp.Value))) // sent a qotd? send if notice is before the day the last qotd was sent
            {
                await SendNotice((await d.GetQotdChannelAsync()), d.latestAvailableNotice);
            }
        }
        private static async Task SendNotice(DiscordChannel qotdChannel, Notices.Notice notice)
        {
            DiscordMessageBuilder noticeMessageBuilder = new();
            DiscordEmbedBuilder noticeEmbed = MessageHelpers.GenericEmbed(
                notice.IsImportant ? "Important Notice" : "Notice",
                notice.NoticeText, 
                color:(notice.IsImportant ? "#ef5658" : "#56efda"));

            noticeEmbed.WithFooter("Authored by the developer \x2022 Configure with /config set notices_level");
            noticeMessageBuilder.AddEmbed(noticeEmbed);

            await qotdChannel.SendMessageAsync(noticeMessageBuilder);
        }

        private static async Task PinMessageIfEnabled(SendQotdData d, DiscordMessage sentMessage)
        {
            if (!d.config.EnableQotdPinMessage)
                return;

            DiscordChannel qotdChannel = await d.GetQotdChannelAsync();

            DiscordMessage? oldSentMessage = null;
            if (d.config.LastQotdMessageId != null)
            {
                try
                {
                    oldSentMessage = await qotdChannel.GetMessageAsync(d.config.LastQotdMessageId.Value);
                }
                catch (NotFoundException)
                {
                    oldSentMessage = null;
                }
            }

            if (oldSentMessage is not null)
            {
                await oldSentMessage.UnpinAsync();
            }

            await sentMessage.PinAsync();
        }
        private static async Task CreateThreadIfEnabled(SendQotdData d, DiscordMessage sentMessage, int? sentQuestionsCount)
        {
            if (!d.config.EnableQotdCreateThread)
                return;

            await sentMessage.CreateThreadAsync($"QOTD{(sentQuestionsCount is null ? "" : $" #{sentQuestionsCount}")} Discussion ({DateTime.UtcNow:yyyy-MM-dd})", DiscordAutoArchiveDuration.Day, reason:"Automatic QOTD thread");
        }

        private static async Task AddPingRoleIfEnabledAndExistent(SendQotdData d, DiscordMessageBuilder builder)
        {
            DiscordRole? pingRole = null;
            if (d.config.QotdPingRoleId == null)
            {
                return;
            }

            try
            {
                pingRole = await d.guild.GetRoleAsync(d.config.QotdPingRoleId.Value);
            }
            catch (NotFoundException)
            {
                await (await d.GetQotdChannelAsync()).SendMessageAsync(
                    MessageHelpers.GenericWarningEmbed("QOTD ping role is set, but not found.\n\n" +
                    "*It can be set using `/config set qotd_ping_role [channel]`, or unset using `/config reset qotd_ping_role`.*")
                    );
                return;
            }

            builder.WithContent(pingRole.Mention);
            builder.WithAllowedMention(new RoleMention(pingRole));
        }
    }
}
