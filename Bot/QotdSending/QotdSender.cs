using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Helpers;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace OpenQotd.Bot.QotdSending
{
    /// <summary>
    /// Sends QOTD messages to guilds based on their configuration and available questions.
    /// </summary>
    public class QotdSender
    {
        /// <summary>
        /// Fetches the guild by ID and tries to send the next QOTD.
        /// </summary>
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
                using AppDbContext dbContext = new();
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

        /// <summary>
        /// Sends the next QOTD to the specified guild.
        /// </summary>
        /// <exception cref="QotdSendException"></exception>
        public static async Task SendNextQotdAsync(DiscordGuild guild, Notices.Notice? latestAvaliableNotice)
        {
            await SendQotdAsync(guild, await QotdSenderHelpers.GetRandomQotd(guild.Id), latestAvaliableNotice);
        }

        /// <summary>
        /// Sends the specified QOTD or a preset/unavailable message if null.
        /// </summary>
        public static async Task SendQotdAsync(DiscordGuild guild, Question? question, Notices.Notice? latestAvailableNotice)
        {
            // Fetch the config and update the last sent timestamp
            Config? config;
            DateTime? previousLastSentTimestamp;
            using (AppDbContext dbContext = new())
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

            // Try to send a question if available
            if (question != null)
            {
                await SendQotdQuestionAsync(sendQotdData, question);
                return;
            }

            // If no question is available, try to send a preset if enabled and available
            if (config.EnableQotdAutomaticPresets)
            {
                List<PresetSent> presetSents;
                using (AppDbContext dbContext = new())
                {
                    presetSents = await dbContext.PresetSents
                        .Where(ps => ps.GuildId == guild.Id)
                        .ToListAsync();
                }

                if (presetSents.Count < Presets.Values.Length)
                {
                    await SendQotdPresetAsync(sendQotdData, presetSents);
                    return;
                }
            }

            // If no question or preset is available, send the unavailable message if enabled
            await SendQotdUnavailableMessageIfEnabledAsync(sendQotdData);
        }
        /// <summary>
        /// Tries to send the "no QOTD available" message if enabled in config.
        /// </summary>
        private static async Task SendQotdUnavailableMessageIfEnabledAsync(SendQotdData d)
        {
            if (!d.config.EnableQotdUnavailableMessage)
                return;

            DiscordChannel qotdChannel = await d.GetQotdChannelAsync();

            DiscordMessageBuilder messageBuilder = new();

            messageBuilder.AddEmbed(
                GenericEmbeds.Custom(title: $"No {d.config.QotdTitle ?? Config.DEFAULT_QOTD_TITLE} Available", message: $"There is currently no {d.config.QotdTitle ?? Config.DEFAULT_QOTD_TITLE}" +
                $"{(d.config.EnableQotdAutomaticPresets ? " or Preset question" : "")} available." +
                (d.config.EnableSuggestions ? $"\n\n*Suggest some using `/qotd`!*" : ""), color: "#dc5051"));
            QotdSenderHelpers.AddSuggestButtonIfEnabled(d.config, messageBuilder);

            await qotdChannel.SendMessageAsync(messageBuilder);
            await SendNoticeIfAvailable(d);
        }
        /// <summary>
        /// Tries to send a preset QOTD. Does not check if presets are enabled or available.
        /// </summary>
        /// <returns>Whether or not sending was successful</returns>
        private static async Task SendQotdPresetAsync(SendQotdData d, List<PresetSent> presetSents)
        {
            DiscordChannel qotdChannel = await d.GetQotdChannelAsync();

            int presetIndex = QotdSenderHelpers.GetRandomPreset(presetSents);
            int presetsAvailable = Presets.Values.Length - presetSents.Count;

            DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbed(
                GenericEmbeds.Custom($"{d.config.QotdTitle ?? Config.DEFAULT_QOTD_TITLE}",
                $"**{Presets.Values[presetIndex]}**\n" +
                $"\n" +
                $"*Preset Question*",
                color: "#8acfac")
                .WithFooter($"{presetsAvailable - 1} preset{(presetsAvailable - 1 == 1 ? "" : "s")} left{(d.config.EnableSuggestions ? $", /qotd to suggest" : "")} \x2022 Preset ID: {presetIndex}")
                );
            await QotdSenderHelpers.AddPingRoleIfEnabledAndExistent(d, messageBuilder);

            QotdSenderHelpers.AddSuggestButtonIfEnabled(d.config, messageBuilder);

            DiscordMessage sentMessage = await qotdChannel.SendMessageAsync(messageBuilder);

            using (AppDbContext dbContext = new())
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
            await QotdSenderHelpers.PinMessageIfEnabled(d, sentMessage);
            await QotdSenderHelpers.CreateThreadIfEnabled(d, sentMessage, null);
        }
        /// <summary>
        /// Send a custom QOTD question to the specified guild.
        /// </summary>
        private static async Task SendQotdQuestionAsync(SendQotdData d, Question question)
        {
            DiscordMessageBuilder messageBuilder = new();

            await QotdSenderHelpers.AddPingRoleIfEnabledAndExistent(d, messageBuilder); 

            QotdSenderHelpers.AddSuggestButtonIfEnabled(d.config, messageBuilder);

            int acceptedQuestionsCount;
            int sentQuestionsCount;
            using (AppDbContext dbContext = new())
            {
                acceptedQuestionsCount = await dbContext.Questions.Where(q => q.GuildId == d.guild.Id && q.Type == QuestionType.Accepted).CountAsync()
                    - 1;
                sentQuestionsCount = await dbContext.Questions.Where(q => q.GuildId == d.guild.Id && q.Type == QuestionType.Sent).CountAsync()
                    + 1;
            }

            messageBuilder.AddEmbed(
                GenericEmbeds.Custom($"{d.config.QotdTitle ?? Config.DEFAULT_QOTD_TITLE} #{sentQuestionsCount}",
                $"**{question.Text}**\n" +
                $"\n" +
                $"*Submitted by <@{question.SubmittedByUserId}>*",
                color: "#8acfac")
                .WithFooter($"{acceptedQuestionsCount} question{(acceptedQuestionsCount == 1 ? "" : "s")} left{(d.config.EnableSuggestions ? $", /qotd to suggest" : "")} \x2022 Question ID: {question.GuildDependentId}")
                );

            DiscordChannel qotdChannel = await d.GetQotdChannelAsync();

            // Send the message
            DiscordMessage sentMessage = await qotdChannel.SendMessageAsync(messageBuilder);
            using (AppDbContext dbContext = new())
            {
                // Update the question and config in the database
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

            // Warn if this was the last question available
            if (acceptedQuestionsCount == 0)
            {
                DiscordMessageBuilder lastQuestionWarning = new();

                int presetsSent;
                using (AppDbContext dbContext = new())
                {
                    presetsSent = await dbContext.PresetSents.Where(ps => ps.GuildId == d.guild.Id).CountAsync();
                }
                int presetsLeft = Presets.Values.Length - presetsSent;

                // TODO: Enable once #65 is added
                //lastQuestionWarning.AddEmbed(
                //    GenericEmbeds.Warning(title: $"Warning: Last {d.config.QotdTitle ?? Config.DEFAULT_QOTD_TITLE}", message:
                //    "There is no more Accepted QOTD of this server left." +
                //    (presetsLeft > 0 && d.config.EnableQotdAutomaticPresets ? $"\nIf none are added, one of **{presetsLeft} Presets** will start to be used instead." : "") +
                //    (d.config.EnableSuggestions ? $"\n\n*More can be suggested using `/qotd`!*" : "")));

                await qotdChannel.SendMessageAsync(lastQuestionWarning);
            }

            await SendNoticeIfAvailable(d);

            await QotdSenderHelpers.PinMessageIfEnabled(d, sentMessage);
            await QotdSenderHelpers.CreateThreadIfEnabled(d, sentMessage, sentQuestionsCount);
        }

        /// <summary>
        /// If a recent notice is available and the config allows it, send it to the guild's QOTD channel.
        /// </summary>
        private static async Task SendNoticeIfAvailable(SendQotdData d)
        {
            if (d.latestAvailableNotice is null)
                return;

            if (d.config.NoticesLevel == Config.NoticeLevel.None)
                return;

            if (d.config.NoticesLevel == Config.NoticeLevel.Important && !d.latestAvailableNotice.IsImportant)
                return;

            if (d.previousLastSentTimestamp is null && d.latestAvailableNotice.Date >= DateTime.UtcNow.AddDays(-2) || // not sent a qotd yet? send if notice is less than 2 days old
                d.previousLastSentTimestamp is not null && d.latestAvailableNotice.Date > d.previousLastSentTimestamp.Value && d.latestAvailableNotice.Date >= DateTime.UtcNow.AddDays(-7)) // sent a qotd? send if notice is before the day the last qotd was sent if it is less than 7 days old
            {
                await SendNotice(await d.GetQotdChannelAsync(), d.latestAvailableNotice);
            }
        }
        /// <summary>
        /// Sends a specified notice to the specified channel.
        /// </summary>
        private static async Task SendNotice(DiscordChannel qotdChannel, Notices.Notice notice)
        {
            DiscordMessageBuilder noticeMessageBuilder = new();
            DiscordEmbedBuilder noticeEmbed = GenericEmbeds.Custom(
                notice.IsImportant ? "Important Notice" : "Notice",
                notice.NoticeText, 
                color:notice.IsImportant ? "#ef5658" : "#56efda");

            noticeEmbed.WithFooter("Authored by the developer \x2022 Configure with /config set notices_level");
            noticeMessageBuilder.AddEmbed(noticeEmbed);

            await qotdChannel.SendMessageAsync(noticeMessageBuilder);
        }
    }
}
