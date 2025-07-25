﻿using CustomQotd.Database;
using CustomQotd.Database.Entities;
using CustomQotd.Features.Helpers;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CustomQotd.Features.QotdSending
{
    public class QotdSender
    {
        private static Random random = new Random();

        public static async Task<Question?> GetRandomQotd(ulong guildId)
        {
            Question[] questions;
            using (var dbContext = new AppDbContext())
            {
                questions = await dbContext.Questions
                    .Where(q => q.GuildId == guildId && q.Type == QuestionType.Accepted)
                    .ToArrayAsync();
            }

            if (questions == null || questions.Length == 0)
                return null;

            return questions[random.Next(questions.Length)];
        }
        public static async Task<int> GetRandomPreset(ulong guildId)
        {
            HashSet<PresetSent> presetSents;
            using (var dbContext = new AppDbContext())
            {
                presetSents = (await dbContext.PresetSents
                    .Where(ps => ps.GuildId == guildId)
                    .ToListAsync())
                    .ToHashSet();
            }

            int index = -1;
            int timeToLive = 100_000;

            while ((index == -1 || presetSents.Any(ps => ps.GuildId == guildId && ps.PresetIndex == index)) && timeToLive > 0)
            {
                index = random.Next(Presets.Values.Length);
                timeToLive--;
            }

            if (timeToLive == 0)
            {
                throw new Exception("Time to live has expired (endless loop?)");
            }

            return index;
        }
        
        public static async Task SendNextQotd(ulong guildId, Notices.Notice? latestAvaliableNotice)
        {
            await SendQotd(guildId, await GetRandomQotd(guildId), latestAvaliableNotice);

        }

        /// <summary>
        /// Send a QOTD
        /// </summary>
        /// <returns>Whether the QOTD channel has been found or not. Returns true/false regardless whether question is null or not.</returns>
        public static async Task<bool> SendQotd(ulong guildId, Question? question, Notices.Notice? latestAvailableNotice)
        {
            DiscordGuild guild;
            try
            {
                guild = await Program.Client.GetGuildAsync(guildId);
            }
            catch (NotFoundException)
            {
                using (var dbContext = new AppDbContext())
                {
                    Config? delConfig = await dbContext.Configs.Where(c => c.GuildId == guildId).FirstOrDefaultAsync();

                    if (delConfig != null)
                    {
                        dbContext.Remove(delConfig);
                    }

                    List<Question> delQuestions = await dbContext.Questions.Where(q => q.GuildId == guildId).ToListAsync();

                    dbContext.RemoveRange(delQuestions);

                    await dbContext.SaveChangesAsync();
                }

                return false; 
            }

            Config? config;
            DateTime? previousLastSentTimestamp;
            using (var dbContext = new AppDbContext())
            {
                config = await dbContext.Configs
                    .Where(c => c.GuildId == guildId)
                    .FirstOrDefaultAsync();

                if (config == null)
                    return false;

                previousLastSentTimestamp = config.LastSentTimestamp;
                config.LastSentTimestamp = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();
            }

            DiscordButtonComponent suggestButton = new(DiscordButtonStyle.Secondary, "suggest-qotd", "Suggest a new QOTD");

            DiscordChannel qotdChannel;
            try
            {
                qotdChannel = await guild.GetChannelAsync(config.QotdChannelId);
            }
            catch (NotFoundException)
            {
                return false;
            }

            if (question == null)
            {
                int presetsAvailable = 0;
                if (config.EnableQotdAutomaticPresets)
                {
                    using (var dbContext = new AppDbContext())
                    {
                        presetsAvailable = Presets.Values.Length -
                            await dbContext.PresetSents.Where(ps => ps.GuildId == guildId).CountAsync();
                    }
                }
                if (presetsAvailable > 0)
                {
                    int presetIndex = await GetRandomPreset(guildId);

                    DiscordMessageBuilder presetMessageBuilder = new();

                    await AddPingRoleIfExistent(presetMessageBuilder, guild, config, qotdChannel);
                    presetMessageBuilder.AddEmbed(
                        MessageHelpers.GenericEmbed($"Question Of The Day",
                        $"**{Presets.Values[presetIndex]}**\n" +
                        $"\n" +
                        $"*Preset Question*",
                        color: "#8acfac")
                        .WithFooter($"{presetsAvailable - 1} preset{((presetsAvailable - 1) == 1 ? "" : "s")} left{(config.EnableSuggestions ? $", /qotd to suggest" : "")} \x2022 Preset ID: {presetIndex}")
                        );
                    
                    if (config.EnableSuggestions)
                    {
                        presetMessageBuilder.AddActionRowComponent(suggestButton);
                    }

                    DiscordMessage presetMessage = await qotdChannel.SendMessageAsync(presetMessageBuilder);
                    await SendNoticeIfAvailable(config, qotdChannel, latestAvailableNotice, previousLastSentTimestamp);
                    
                    await PinMessageIfEnabled(config, qotdChannel, presetMessage);
                    await CreateThreadIfEnabled(config, qotdChannel, presetMessage, null);

                    using (var dbContext = new AppDbContext())
                    {
                        Config? foundConfig = await dbContext.Configs.Where(c => c.GuildId == guildId).FirstOrDefaultAsync();
                        if (foundConfig != null)
                        {
                            foundConfig.LastQotdMessageId = presetMessage.Id;
                        }

                        await dbContext.PresetSents.AddAsync(new PresetSent() { GuildId = guildId, PresetIndex = presetIndex });

                        await dbContext.SaveChangesAsync(); 
                    }
                }
                else if (config.EnableQotdUnavailableMessage)
                {
                    DiscordMessageBuilder noQuestionMessage = new();

                    await AddPingRoleIfExistent(noQuestionMessage, guild, config, qotdChannel);
                    noQuestionMessage.AddEmbed(
                        MessageHelpers.GenericEmbed(title: "No QOTD Available", message: $"There is currently no Question Of The Day available." +
                        (config.EnableSuggestions ? $"\n\n*Suggest some using `/qotd`!*" : ""), color: "#dc5051"));
                    if (config.EnableSuggestions)
                    {
                        noQuestionMessage.AddActionRowComponent(suggestButton);
                    }

                    await qotdChannel.SendMessageAsync(noQuestionMessage);
                    await SendNoticeIfAvailable(config, qotdChannel, latestAvailableNotice, previousLastSentTimestamp);
                }

                return true;
            }

            DiscordUser? user;
            try
            {
                user = await Program.Client.GetUserAsync(question.SubmittedByUserId);
            }
            catch (NotFoundException)
            {
                user = null;
            }

            DiscordMessageBuilder qotdMessageBuilder = new();

            await AddPingRoleIfExistent(qotdMessageBuilder, guild, config, qotdChannel);

            int acceptedQuestionsCount;
            int sentQuestionsCount;
            using (var dbContext = new AppDbContext())
            {
                acceptedQuestionsCount = await dbContext.Questions.Where(q => q.GuildId == guildId && q.Type == QuestionType.Accepted).CountAsync()
                    - 1;
                sentQuestionsCount = await dbContext.Questions.Where(q => q.GuildId == guildId && q.Type == QuestionType.Sent).CountAsync()
                    + 1;
            }

            qotdMessageBuilder.AddEmbed(
                MessageHelpers.GenericEmbed($"Question Of The Day #{sentQuestionsCount}",
                $"**{question.Text}**\n" +
                $"\n" +
                $"*Submitted by {(user is not null ? $"{user.Mention}" : $"user with ID `{question.SubmittedByUserId}`")}*",
                color: "#8acfac")
                .WithFooter($"{acceptedQuestionsCount} question{(acceptedQuestionsCount == 1 ? "" : "s")} left{(config.EnableSuggestions ? $", /qotd to suggest" : "")} \x2022 Question ID: {question.GuildDependentId}")
                );

            if (config.EnableSuggestions)
            {
                qotdMessageBuilder.AddActionRowComponent(suggestButton);
            }

            DiscordMessage qotdMessage = await qotdChannel.SendMessageAsync(qotdMessageBuilder);

            if (acceptedQuestionsCount == 0)
            {
                DiscordMessageBuilder lastQuestionWarning = new();

                int presetsSent;
                using (var dbContext = new AppDbContext())
                {
                    presetsSent = await dbContext.PresetSents.Where(ps => ps.GuildId == guildId).CountAsync();
                }
                int presetsLeft = Presets.Values.Length - presetsSent;

                lastQuestionWarning.AddEmbed(
                    MessageHelpers.GenericWarningEmbed(title: "Warning: Last QOTD", message:
                    "There is no more Accepted QOTD of this server left." +
                    (presetsLeft > 0 && config.EnableQotdAutomaticPresets ? $"\nIf none are added, one of **{presetsLeft} Presets** will start to be used instead." : "") +
                    (config.EnableSuggestions ? $"\n\n*More can be suggested using `/qotd`!*" : "")));

                await qotdChannel.SendMessageAsync(lastQuestionWarning);
            }
            await SendNoticeIfAvailable(config, qotdChannel, latestAvailableNotice, previousLastSentTimestamp);

            await PinMessageIfEnabled(config, qotdChannel, qotdMessage);
            await CreateThreadIfEnabled(config, qotdChannel, qotdMessage, sentQuestionsCount);

            using (var dbContext = new AppDbContext())
            {
                Config? foundConfig = await dbContext.Configs.Where(c => c.GuildId == guildId).FirstOrDefaultAsync();
                if (foundConfig != null)
                {
                    foundConfig.LastQotdMessageId = qotdMessage.Id;
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

            return true;
        }

        private static async Task SendNoticeIfAvailable(Config config, DiscordChannel qotdChannel, Notices.Notice? latestAvailableNotice, DateTime? previousLastSentTimestamp)
        {
            if (latestAvailableNotice is null)
                return;

            if (config.NoticesLevel == Config.NoticeLevel.None)
                return;

            if (config.NoticesLevel == Config.NoticeLevel.Important && !latestAvailableNotice.IsImportant)
                return;

            if ((previousLastSentTimestamp is null && (latestAvailableNotice.Date >= DateTime.UtcNow.AddDays(-2))) || // not sent a qotd yet? send if notice is less than 2 days old
                (previousLastSentTimestamp is not null && (latestAvailableNotice.Date > previousLastSentTimestamp.Value))) // sent a qotd? send if notice is before the day the last qotd was sent
            {
                await SendNotice(qotdChannel, latestAvailableNotice);
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

        private static async Task PinMessageIfEnabled(Config config, DiscordChannel qotdChannel, DiscordMessage qotdMessage)
        {
            if (!config.EnableQotdPinMessage)
                return;

            DiscordMessage? oldQotdMessage = null;
            if (config.LastQotdMessageId != null)
            {
                try
                {
                    oldQotdMessage = await qotdChannel.GetMessageAsync(config.LastQotdMessageId.Value);
                }
                catch (NotFoundException)
                {
                    oldQotdMessage = null;
                }
            }

            if (oldQotdMessage is not null)
            {
                await oldQotdMessage.UnpinAsync();
            }

            await qotdMessage.PinAsync();
        }
        private static async Task CreateThreadIfEnabled(Config config, DiscordChannel qotdChannel, DiscordMessage qotdMessage, int? sentQuestionsCount)
        {
            if (!config.EnableQotdCreateThread)
                return;

            await qotdMessage.CreateThreadAsync($"QOTD{(sentQuestionsCount is null ? "" : $" #{sentQuestionsCount}")} Discussion ({DateTime.UtcNow:yyyy-MM-dd})", DiscordAutoArchiveDuration.Day, reason:"Automatic QOTD thread");
        }

        private static async Task AddPingRoleIfExistent(DiscordMessageBuilder builder, DiscordGuild guild, Config config, DiscordChannel onErrorChannel)
        {
            DiscordRole? pingRole = null;
            if (config.QotdPingRoleId == null)
            {
                return;
            }

            try
            {
                pingRole = await guild.GetRoleAsync(config.QotdPingRoleId.Value);
            }
            catch (NotFoundException)
            {
                await onErrorChannel.SendMessageAsync(
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
