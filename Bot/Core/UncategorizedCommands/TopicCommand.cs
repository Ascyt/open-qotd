using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using OpenQotd.Core.Questions.Entities;
using System.ComponentModel;

namespace OpenQotd.Core.UncategorizedCommands
{
    public class TopicCommand
    {
        [Command("topic")]
        [Description("Sends a random Sent QOTD to the current channel.")]
        public static async Task TopicAsync(CommandContext context,
            [Description("Which OpenQOTD profile to take the topic from.")][SlashAutoCompleteProvider<Profiles.AutoCompleteProviders.ViewableProfiles>] int from,
            [Description("Includes all Preset questions if enabled in the config.")] bool includePresets=true)
        {
            int profileId = from;

            Config? config = await Profiles.Api.TryGetConfigAsync(context, from);
            if (config is null || !await Permissions.Api.Basic.UserIsBasic(context, config))
                return;

            includePresets = includePresets && config.EnableQotdAutomaticPresets; // Presets can only be included if enabled in config.

            Question[] questions;
            using (AppDbContext dbContext = new())
            {
                questions = await dbContext.Questions
                    .Where(q => q.ConfigId == config.Id && q.Type == QuestionType.Sent)
                    .ToArrayAsync();
            }

            if (questions.Length == 0 && !includePresets)
            {
                await context.RespondAsync(GenericEmbeds.Error(
                    title: "No Sent QOTDs available",
                    message: "There are no QOTDs of type Sent available."));
                return;
            }

            DiscordButtonComponent rerollButton = new(DiscordButtonStyle.Secondary, "reroll", "⟳");

            DiscordEmbed embed = GetRandomTopic(questions, includePresets);

            DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbed(embed);
            messageBuilder.AddActionRowComponent(
                rerollButton
                );

            DiscordMessageBuilder messageBuilderNoButtons = new();
            messageBuilderNoButtons.AddEmbed(embed);

            await context.RespondAsync(
                    messageBuilder
                );

            DiscordMessage? message = await context.GetResponseAsync();

            if (message is null)
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
                int totalQuestionCount = questions.Length + Presets.Api.Presets.Length;
                if (Random.Shared.Next(totalQuestionCount) >= questions.Length)
                {
                    usePresets = true;
                }
            }

            DiscordEmbed embed;
            if (!usePresets)
            {
                Question question = questions[Random.Shared.Next(questions.Length)];

                embed = GenericEmbeds.Info(
                    title: question.Text!,
                    message: $"*Submitted by: <@!{question.SubmittedByUserId}>*")
                    .WithFooter($"Question ID: {question.GuildDependentId}");
            }
            else
            {
                int presetId = Random.Shared.Next(Presets.Api.Presets.Length);
                string preset = Presets.Api.Presets[presetId];

                embed = GenericEmbeds.Info(
                    title: preset,
                    message: $"*Preset Question*"
                    )
                    .WithFooter($"Preset ID: {presetId}");
            }

            return embed;
        }
    }
}
