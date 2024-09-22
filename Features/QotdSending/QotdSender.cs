using CustomQotd.Database;
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

        public static async Task SendNextQotd(ulong guildId)
        {
            await SendQotd(guildId, await GetRandomQotd(guildId));
        }

        /// <summary>
        /// Send a QOTD
        /// </summary>
        /// <returns>Whether the QOTD channel has been found or not. Returns true/false regardless whether question is null or not.</returns>
        public static async Task<bool> SendQotd(ulong guildId, Question? question)
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
            using (var dbContext = new AppDbContext())
            {
                config = await dbContext.Configs
                    .Where(c => c.GuildId == guildId)
                    .FirstOrDefaultAsync();

                if (config == null)
                    return false;

                config.LastSentDay = DateTime.UtcNow.Day;

                await dbContext.SaveChangesAsync();
            }

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
                if (config.EnableQotdUnavailableMessage)
                {
                    DiscordMessageBuilder noQuestionMessage = new();

                    await AddPingRoleIfExistent(noQuestionMessage, guild, config, qotdChannel);
                    noQuestionMessage.AddEmbed(
                        MessageHelpers.GenericEmbed(title: "No QOTD Available", message: $"There is currently no Question Of The Day available." +
                        (config.EnableSuggestions ? $"\n\n*Suggest some using `/qotd`!*" : ""), color: "#dc5051"));

                    await qotdChannel.SendMessageAsync(noQuestionMessage);
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
            using (var dbContext = new AppDbContext())
            {
                acceptedQuestionsCount = await dbContext.Questions.Where(q => q.GuildId == guildId && q.Type == QuestionType.Accepted).CountAsync();
            }
            int sentQuestionsCount;
            using (var dbContext = new AppDbContext())
            {
                sentQuestionsCount = await dbContext.Questions.Where(q => q.GuildId == guildId && q.Type == QuestionType.Sent).CountAsync();
            }

            qotdMessageBuilder.AddEmbed(
                MessageHelpers.GenericEmbed($"Question Of The Day #{sentQuestionsCount + 1}",
                $"> **{question.Text}**\n" +
                $"\n" +
                $"*Submitted by {(user is not null ? $"{user.Mention}" : $"user with ID `{question.SubmittedByUserId}`")}*",
                color: "#8acfac")
                .WithFooter($"{acceptedQuestionsCount} question{(acceptedQuestionsCount == 1 ? "" : "s")} left{(config.EnableSuggestions ? $", /qotd to suggest" : "")} \x2022 Question ID: {question.GuildDependentId}")
                );

            DiscordMessage qotdMessage = await qotdChannel.SendMessageAsync(qotdMessageBuilder);

            if (config.EnableQotdPinMessage)
            {
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
