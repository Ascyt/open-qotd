using CommunityToolkit.HighPerformance.Helpers;
using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace OpenQotd.Bot.Commands
{
    public class SuggestCommand
    {
        [Command("suggest")]
        [Description("Suggest a Question Of The Day to be added.")]
        public static async Task SuggestAsync(CommandContext context,
            [Description ("The question to be added.")] string question)
        {
            Config? config = await ProfileHelpers.TryGetDefaultConfigAsync(context);
            if (config is null || !await CommandRequirements.UserIsBasic(context, config))
                return;

            if (!await CommandRequirements.IsWithinMaxQuestionsAmount(context, 1))
                return;

            if (!config.EnableSuggestions)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(title: "Suggestions Disabled", message: "Suggesting of QOTDs using `/qotd` or `/suggest` has been disabled by staff."));
                return;
            }

            (bool, DiscordEmbed) result = await SuggestNoContextAsync(question, config, context.Guild!, context.Channel, context.User);

            if (context is SlashCommandContext && result.Item1) // Is slash command and not errored
            {
                SlashCommandContext? slashCommandcontext = context as SlashCommandContext;

                await slashCommandcontext!.RespondAsync(result.Item2, ephemeral: true);
            }
            else
            {
                await context.RespondAsync(result.Item2);
            }
        }

        /// <returns>(whether or not successful, response message)</returns>
        public static async Task<(bool, DiscordEmbed)> SuggestNoContextAsync(string question, Config config, DiscordGuild guild, DiscordChannel discordChannel, DiscordUser user)
        {
            if (!await Question.CheckTextValidity(question, null, config))
                return (false, GenericEmbeds.Error("Text validity check failed"));

            Question newQuestion;
            using (AppDbContext dbContext = new())
            {
                newQuestion = new Question()
                {
                    ConfigId = config.Id,
                    GuildId = guild.Id,
                    GuildDependentId = await Question.GetNextGuildDependentId(config),
                    Type = QuestionType.Suggested,
                    Text = question,
                    SubmittedByUserId = user.Id,
                    Timestamp = DateTime.UtcNow
                };
                await dbContext.Questions.AddAsync(newQuestion);
                await dbContext.SaveChangesAsync();
            }

            (bool, DiscordEmbedBuilder) result = (true, GenericEmbeds.Success("QOTD Suggested!",
                    $"Your Question Of The Day:\n" +
                    $"\"**{newQuestion.Text}**\"\n" +
                    $"\n" +
                    $"Has successfully been suggested!\n" +
                    $"You will be notified when it gets accepted or denied."));


            await Logging.LogUserAction(discordChannel, user, config, "Suggested QOTD", $"\"**{newQuestion.Text}**\"\nID: `{newQuestion.GuildDependentId}`");

            if (config.SuggestionsChannelId is null)
            {
                if (config.SuggestionsPingRoleId is null)
                {
                    await discordChannel.SendMessageAsync(GenericEmbeds.Warning("Suggestions ping role is set, but suggestions channel is not.\n\n" +
                        "*The channel can be set using `/config set suggestions_channel [channel]`, or the ping role can be removed using `/config reset suggestions_ping_role`.*"));
                }
                // Qotd suggested but no suggestion channel
                return result;
            }

            DiscordRole? pingRole = null;
            if (config.SuggestionsPingRoleId is not null)
            {
                try
                {
                    pingRole = await guild.GetRoleAsync(config.SuggestionsPingRoleId.Value);
                }
                catch (NotFoundException)
                {
                    await discordChannel.SendMessageAsync(
                        GenericEmbeds.Warning("Suggestions ping role is set, but not found.\n\n" +
                        "*It can be set using `/config set suggestions_ping_role [channel]`, or unset using `/config reset suggestions_ping_role`.*")
                        );
                }
            }

            DiscordChannel channel;
            try
            {
                channel = await guild.GetChannelAsync(config.SuggestionsChannelId.Value);
            }
            catch (NotFoundException)
            {
                return (false,
                    GenericEmbeds.Warning("Suggestions channel is set, but not found.\n\n" +
                    "*It can be set using `/config set suggestions_channel [channel]`, or unset using `/config reset suggestions_channel`.*")
                    );
            }

            DiscordMessageBuilder messageBuilder = new();

            AddPingIfAvailable(messageBuilder, pingRole);

            string embedBody = $"\"**{newQuestion.Text}**\"\n" +
                $"By: {user.Mention} (`{user.Id}`)\n" +
                $"ID: `{newQuestion.GuildDependentId}`";

            messageBuilder.AddEmbed(GenericEmbeds.Custom("A new QOTD Suggestion is available!", embedBody,
                color: "#f0b132"));

            messageBuilder.AddActionRowComponent(
                new DiscordButtonComponent(DiscordButtonStyle.Success, $"suggestions-accept/{config.ProfileId}/{newQuestion.GuildDependentId}", "Accept"),
                new DiscordButtonComponent(DiscordButtonStyle.Danger, $"suggestions-deny/{config.ProfileId}/{newQuestion.GuildDependentId}", "Deny")
            );

            DiscordMessage message = await channel.SendMessageAsync(
                    messageBuilder
                );

            await message.PinAsync();

            using (AppDbContext dbContext = new())
            {
                Question? updateQuestion = await dbContext.Questions.FindAsync(newQuestion.Id);

                if (updateQuestion != null)
                {
                    updateQuestion.SuggestionMessageId = message.Id;

                    await dbContext.SaveChangesAsync();

                    newQuestion = updateQuestion;
                }
            }

            return result;
        }
        private static void AddPingIfAvailable(DiscordMessageBuilder messageBuilder, DiscordRole? pingRole)
        {
            if (pingRole is not null)
            {
                messageBuilder.WithContent(pingRole.Mention);
                messageBuilder.WithAllowedMention(new RoleMention(pingRole));
            }
        }


        [Command("qotd")]
        [Description("Suggest a Question Of The Day to be added.")]
        public static async Task QotdAsync(CommandContext context,
            [Description("The question to be added.")] string question)
            => await SuggestAsync(context, question);
    }
}
