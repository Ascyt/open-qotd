using CustomQotd.Database;
using CustomQotd.Database.Entities;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Threading.Channels;

namespace CustomQotd.Features.Commands
{
    public class TopicCommand
    {
        private static Random random = new Random();

        [Command("topic")]
        [Description("Send a random Sent QOTD to the current channel.")]
        public static async Task TopicAsync(CommandContext context,
            [Description("Whether or not to include all existing Preset questions.")] bool includePresets=true)
        {
            if (!await CommandRequirements.IsConfigInitialized(context) || !await CommandRequirements.UserIsBasic(context))
                return;

            Question[] questions;
            using (var dbContext = new AppDbContext())
            {
                questions = await dbContext.Questions
                    .Where(q => q.GuildId == context.Guild!.Id && q.Type == QuestionType.Sent)
                    .ToArrayAsync();
            }

            if (questions.Length == 0 && !includePresets)
            {
                await context.RespondAsync(MessageHelpers.GenericErrorEmbed(
                    title: "No Sent QOTDs available",
                    message: "There are no QOTDs of type Sent available."));
                return;
            }

            DiscordButtonComponent rerollButton = new DiscordButtonComponent(DiscordButtonStyle.Secondary, "reroll", "⟳");

            DiscordEmbed embed = GetRandomTopic(questions, includePresets);

            DiscordMessageBuilder messageBuilder = new DiscordMessageBuilder();
            messageBuilder.AddEmbed(embed);
            messageBuilder.AddComponents(
                rerollButton
                );

            DiscordMessageBuilder messageBuilderNoButtons = new DiscordMessageBuilder();
            messageBuilderNoButtons.AddEmbed(embed);

            await context.RespondAsync(
                    messageBuilder
                );

            DiscordMessage? message = await context.GetResponseAsync();

            if (message == null)
            {
                return;
            }

            while (true)
            {
                InteractivityResult<ComponentInteractionCreatedEventArgs>? resultNullable = null;
                resultNullable = await message.WaitForButtonAsync();
                if (resultNullable.Value.TimedOut)
                {
                    await message.ModifyAsync(messageBuilderNoButtons);
                    return;
                }

                var result = resultNullable!.Value;

                if (result.Result.User.Id != context.User.Id)
                {
                    continue;
                }

                if (result.Result.Id != "reroll")
                {
                    await message.ModifyAsync(messageBuilderNoButtons);
                    return;
                }

                embed = GetRandomTopic(questions, includePresets);

                messageBuilder = new DiscordMessageBuilder();
                messageBuilder.AddEmbed(embed);
                messageBuilder.AddComponents(
                    rerollButton
                    );

                messageBuilderNoButtons = new DiscordMessageBuilder();
                messageBuilderNoButtons.AddEmbed(embed);

                DiscordInteractionResponseBuilder interactionResponseBuilder = new DiscordInteractionResponseBuilder();
                interactionResponseBuilder.AddEmbed(embed);
                interactionResponseBuilder.AddComponents(
                    rerollButton
                    );

                await result.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, interactionResponseBuilder);
            }
        }

        private static DiscordEmbed GetRandomTopic(Question[] questions, bool includePresets)
        {
            bool usePresets = false;

            if (includePresets)
            {
                int totalQuestionCount = questions.Length + Presets.Values.Length;
                if (random.Next(totalQuestionCount) >= questions.Length)
                {
                    usePresets = true;
                }
            }

            DiscordEmbed embed;
            if (!usePresets)
            {
                Question question = questions[random.Next(questions.Length)];

                embed = MessageHelpers.GenericEmbed(
                    title: question.Text,
                    message: $"*Submitted by: <@!{question.SubmittedByUserId}>*")
                    .WithFooter($"Question ID: {question.GuildDependentId}");
            }
            else
            {
                int presetId = random.Next(Presets.Values.Length);
                string preset = Presets.Values[presetId];

                embed = MessageHelpers.GenericEmbed(
                    title: preset,
                    message: $"*Preset Question*"
                    )
                    .WithFooter($"Preset ID: {presetId}");
            }

            return embed;
        }
    }
}
