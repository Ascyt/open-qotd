using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using OpenQotd.Core.Presets.Entities;
using OpenQotd.Core.Questions.Entities;

namespace OpenQotd.Core.QotdSending.Sender
{
    /// <summary>
    /// Sends QOTD messages to guilds based on their configuration and available questions.
    /// </summary>
    public class Api
    {
        /// <summary>
        /// Fetches the guild by ID and tries to send the next QOTD.
        /// </summary>
        /// <returns>Whether or not the guild was found.</returns>
        /// <exception cref="QotdSendException"></exception>
        public static async Task<bool> FetchGuildAndSendNextQotdAsync(Config config, Notices.Api.Notice? latestAvailableNotice)
        {
            DiscordGuild guild;
            try
            {
                guild = await Program.Client.GetGuildAsync(config.GuildId);
            }
            catch (NotFoundException)
            {
                using AppDbContext dbContext = new();
                Config? foundConfig = await dbContext.Configs
                    .FindAsync(config.Id);

                if (foundConfig is null)
                    return false;

                // If the guild is not found, disable automatic QOTD sending for it
                // This is to avoid repeated errors when cache is re-calculated like for startup
                foundConfig.EnableAutomaticQotd = false;

                await dbContext.SaveChangesAsync();
                return false;
            }

            await SendNextQotdAsync(guild, config, latestAvailableNotice);
            return true;
        }

        /// <summary>
        /// Sends the next QOTD to the specified guild.
        /// </summary>
        /// <exception cref="QotdSendException"></exception>
        public static async Task SendNextQotdAsync(DiscordGuild guild, Config config, Notices.Api.Notice? latestAvaliableNotice)
        {
            await SendQotdAsync(guild, config, await Helpers.GetRandomQotd(config), latestAvaliableNotice);
        }

        /// <summary>
        /// Sends the specified QOTD or a preset/unavailable message if null.
        /// </summary>
        public static async Task SendQotdAsync(DiscordGuild guild, Config config, Question? question, Notices.Api.Notice? latestAvailableNotice)
        {
            // Fetch the config and update the last sent timestamp
            DateTime? previousLastSentTimestamp;
            using (AppDbContext dbContext = new())
            {
                Config? foundConfig = await dbContext.Configs
                    .FindAsync(config.Id);

                if (foundConfig == null)
                    return;

                config = foundConfig;

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
                        .Where(ps => ps.ConfigId == config.Id)
                        .ToListAsync();
                }

                if (presetSents.Count < Presets.Api.Presets.Length)
                {
                    await SendQotdPresetAsync(sendQotdData, presetSents);
                    return;
                }
            }

            // If no question or preset is available, send the unavailable message if enabled
            await SendQotdUnavailableMessageIfEnabledAsync(sendQotdData, question);
        }
        /// <summary>
        /// Tries to send the "no QOTD available" message if enabled in config.
        /// </summary>
        private static async Task SendQotdUnavailableMessageIfEnabledAsync(SendQotdData d, Question? question)
        {
            if (!d.config.EnableQotdUnavailableMessage)
                return;

            DiscordChannel qotdChannel = await d.GetQotdChannelAsync();

            DiscordMessageBuilder messageBuilder = new();

            messageBuilder.AddEmbed(
                GenericEmbeds.Custom(title: $"No {d.QotdShorthand} Available", message: $"There is currently no {d.QotdTitle}" +
                $"{(d.config.EnableQotdAutomaticPresets ? " or Preset question" : "")} available." +
                (d.config.EnableSuggestions ? $"\n\n*Suggest some using `{d.SuggestCommand}`!*" : ""), color: "#dc5051"));
            Helpers.AddButtonsIfEnabled(d.config, question, messageBuilder);

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

            int presetIndex = Helpers.GetRandomPreset(presetSents);
            int presetsAvailable = Presets.Api.Presets.Length - presetSents.Count;

            DiscordEmbedBuilder presetEmbed = 
                GenericEmbeds.Custom($"{d.QotdTitle}",
                $"{Presets.Api.Presets[presetIndex]}" + (d.config.EnableQotdShowCredit ? (
                    $"\n\n" +
                    $"*Preset Question*") : ""
                    ),
                color: d.config.QotdEmbedColorHexEffective);

            if (d.config.EnableQotdShowFooter)
                presetEmbed.WithFooter(
                $"{presetsAvailable - 1} preset{(presetsAvailable - 1 == 1 ? "" : "s")} left{(d.config.EnableSuggestions ? $", {d.SuggestCommand} to suggest" : "")} \x2022 Preset ID: {presetIndex}");

            DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbed(presetEmbed);
            await Helpers.AddPingRoleIfEnabledAndExistent(d, messageBuilder);

            Helpers.AddButtonsIfEnabled(d.config, null, messageBuilder);

            DiscordMessage sentMessage = await qotdChannel.SendMessageAsync(messageBuilder);

            using (AppDbContext dbContext = new())
            {
                Config? foundConfig = await dbContext.Configs
                    .FindAsync(d.config.Id);

                if (foundConfig is not null)
                {
                    foundConfig.LastQotdMessageId = sentMessage.Id;
                }

                await dbContext.PresetSents.AddAsync(new PresetSent() { ConfigId = d.config.Id, GuildId = d.guild.Id, PresetIndex = presetIndex });

                await dbContext.SaveChangesAsync();
            }

            await SendNoticeIfAvailable(d);
            await Helpers.PinMessageIfEnabled(d, sentMessage);
            await Helpers.CreateThreadIfEnabled(d, sentMessage, null);
        }
        /// <summary>
        /// Send a custom QOTD question to the specified guild.
        /// </summary>
        private static async Task SendQotdQuestionAsync(SendQotdData d, Question question)
        {
            DiscordMessageBuilder messageBuilder = new();

            await Helpers.AddPingRoleIfEnabledAndExistent(d, messageBuilder); 

            Helpers.AddButtonsIfEnabled(d.config, question, messageBuilder);

            int acceptedQuestionsCount;
            int sentQuestionsCount;
            using (AppDbContext dbContext = new())
            {
                acceptedQuestionsCount = await dbContext.Questions
                    .Where(q => q.ConfigId == d.config.Id && q.Type == QuestionType.Accepted)
                    .CountAsync()
                    - 1;
                sentQuestionsCount = await dbContext.Questions
                    .Where(q => q.ConfigId == d.config.Id && q.Type == QuestionType.Sent)
                    .CountAsync()
                    + 1;
            }

            DiscordEmbedBuilder qotdEmbed =
                GenericEmbeds.Custom($"{d.QotdTitle}{(d.config.EnableQotdShowCounter ? $" #{sentQuestionsCount}" : "")}",
                $"{question.Text}" + (d.config.EnableQotdShowCredit ? (
                    $"\n\n" +
                    $"*Submitted by <@{question.SubmittedByUserId}>*") : ""
                    ),
                color: d.config.QotdEmbedColorHexEffective);
            if (d.config.EnableQotdShowFooter) 
                qotdEmbed.WithFooter($"{acceptedQuestionsCount} question{(acceptedQuestionsCount == 1 ? "" : "s")} left{(d.config.EnableSuggestions ? $", {d.SuggestCommand} to suggest" : "")} \x2022 Question ID: {question.GuildDependentId}");

            if (question.ThumbnailImageUrl is not null)
                qotdEmbed.WithThumbnail(question.ThumbnailImageUrl);
            messageBuilder.AddEmbed(qotdEmbed);

            DiscordChannel qotdChannel = await d.GetQotdChannelAsync();

            // Send the message
            DiscordMessage sentMessage = await qotdChannel.SendMessageAsync(messageBuilder);
            using (AppDbContext dbContext = new())
            {
                // Update the question and config in the database
                Config? foundConfig = await dbContext.Configs
                    .FindAsync(d.config.Id);
                if (foundConfig != null)
                {
                    foundConfig.LastQotdMessageId = sentMessage.Id;
                }

                Question? foundQuestion = await dbContext.Questions
                    .FindAsync(question.Id);

                if (foundQuestion != null)
                {
                    question.SentNumber = sentQuestionsCount + 1;
                    question.SentTimestamp = DateTime.UtcNow;
                    await AlterQuestionAfterSent(dbContext, d, foundQuestion);
                }

                await dbContext.SaveChangesAsync();
            }

            // Warn if this was the last question available and the config allows it
            if (acceptedQuestionsCount == 0 && d.config.EnableQotdLastAvailableWarn)
            {
                DiscordMessageBuilder lastQuestionWarning = new();

                int presetsSent;
                using (AppDbContext dbContext = new())
                {
                    presetsSent = await dbContext.PresetSents.Where(ps => ps.ConfigId == d.config.Id).CountAsync();
                }
                int presetsLeft = Presets.Api.Presets.Length - presetsSent;

                // TODO: Enable once #65 is added
                lastQuestionWarning.AddEmbed(
                    GenericEmbeds.Warning(title: $"Warning: Last {d.QotdShorthand}", message:
                    $"There is no more Accepted {d.QotdTitle} of this server left." +
                    (presetsLeft > 0 && d.config.EnableQotdAutomaticPresets ? $"\nIf none are added, one of **{presetsLeft} Presets** will start to be used instead." : "") +
                    (d.config.EnableSuggestions ? $"\n\n*More can be suggested using `{d.SuggestCommand}`!*" : "")));
                await qotdChannel.SendMessageAsync(lastQuestionWarning);
            }

            await SendNoticeIfAvailable(d);

            await Helpers.PinMessageIfEnabled(d, sentMessage);
            await Helpers.CreateThreadIfEnabled(d, sentMessage, sentQuestionsCount);
        }

        private static async Task AlterQuestionAfterSent(AppDbContext dbContext, SendQotdData d, Question question)
        {
            Config.AlterQuestionAfterSentOption option = d.config.QotdAlterQuestionAfterSent;

            switch (option)
            {
                case Config.AlterQuestionAfterSentOption.QuestionToSent:
                default:
                    question.Type = QuestionType.Sent;
                    return;
                case Config.AlterQuestionAfterSentOption.QuestionToSentAndResetIfEmpty:
                    question.Type = QuestionType.Sent;

                    if (!await dbContext.Questions
                        .Where(q => q.ConfigId == d.config.Id && q.Id != question.Id)
                        .AnyAsync(q => q.Type == QuestionType.Accepted))
                    {
                        await dbContext
                            .Questions
                            .Where(q => q.ConfigId == d.config.Id && q.Type == QuestionType.Sent)
                            .ExecuteUpdateAsync(q => q.SetProperty(q => q.Type, QuestionType.Accepted));

                        // Reload the question entity to ensure in-memory state matches the database
                        await dbContext.Entry(question).ReloadAsync();
                    }
                    return;
                case Config.AlterQuestionAfterSentOption.QuestionStaysAccepted:
                    return;
                case Config.AlterQuestionAfterSentOption.QuestionToSuggested:
                    question.Type = QuestionType.Suggested;
                    await Suggestions.Helpers.General.TryResetSuggestionMessageIfEnabledAsync(question, d.config, d.guild);
                    return;
                case Config.AlterQuestionAfterSentOption.RemoveQuestion:
                    if (d.config.EnableDeletedToStash)
                    {
                        question.Type = QuestionType.Stashed;
                    }
                    else
                    {
                        dbContext.Questions.Remove(question);
                    }
                    return;
            }
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
        private static async Task SendNotice(DiscordChannel qotdChannel, Notices.Api.Notice notice)
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
