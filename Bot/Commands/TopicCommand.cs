using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Threading.Channels;

namespace OpenQotd.Bot.Commands
{
    public class TopicCommand
    {
        private static readonly Random _random = new();

        [Command("topic")]
        [Description("Send a random Sent QOTD to the current channel.")]
        public static async Task TopicAsync(CommandContext context,
            [Description("Whether or not to include all existing Preset questions.")] bool includePresets=true)
        {
            if (!await CommandRequirements.UserIsBasic(context, null))
                return;

            Question[] questions;
            using (AppDbContext dbContext = new())
            {
                questions = await dbContext.Questions
                    .Where(q => q.ConfigId == context.Guild!.Id && q.Type == QuestionType.Sent)
                    .ToArrayAsync();
            }

            if (questions.Length == 0 && !includePresets)
            {
                await context.RespondAsync(GenericEmbeds.Error(
                    title: "No Sent QOTDs available",
                    message: "There are no QOTDs of type Sent available."));
                return;
            }

            DiscordButtonComponent rerollButton = new DiscordButtonComponent(DiscordButtonStyle.Secondary, "reroll", "⟳");

            DiscordEmbed embed = GetRandomTopic(questions, includePresets);

            DiscordMessageBuilder messageBuilder = new DiscordMessageBuilder();
            messageBuilder.AddEmbed(embed);
            messageBuilder.AddActionRowComponent(
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

            int ttl = 64;
            while (ttl > 0)
            {
                ttl--;

                InteractivityResult<ComponentInteractionCreatedEventArgs>? resultNullable = null;
                resultNullable = await message.WaitForButtonAsync();
                if (resultNullable.Value.TimedOut)
                {
                    await message.ModifyAsync(messageBuilderNoButtons);
                    return;
                }

                InteractivityResult<ComponentInteractionCreatedEventArgs> result = resultNullable!.Value;

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
                messageBuilder.AddActionRowComponent(
                    rerollButton
                    );

                messageBuilderNoButtons = new DiscordMessageBuilder();
                messageBuilderNoButtons.AddEmbed(embed);

                DiscordInteractionResponseBuilder interactionResponseBuilder = new();
                interactionResponseBuilder.AddEmbed(embed);
                interactionResponseBuilder.AddActionRowComponent(
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
                if (_random.Next(totalQuestionCount) >= questions.Length)
                {
                    usePresets = true;
                }
            }

            DiscordEmbed embed;
            if (!usePresets)
            {
                Question question = questions[_random.Next(questions.Length)];

                embed = GenericEmbeds.Custom(
                    title: question.Text!,
                    message: $"*Submitted by: <@!{question.SubmittedByUserId}>*")
                    .WithFooter($"Question ID: {question.GuildDependentId}");
            }
            else
            {
                int presetId = _random.Next(Presets.Values.Length);
                string preset = Presets.Values[presetId];

                embed = GenericEmbeds.Custom(
                    title: preset,
                    message: $"*Preset Question*"
                    )
                    .WithFooter($"Preset ID: {presetId}");
            }

            return embed;
        }
    }
}
