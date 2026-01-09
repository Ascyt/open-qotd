using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using OpenQotd.Core.Questions.Entities;

namespace OpenQotd.Core.Questions.Commands
{
    public sealed partial class QuestionsCommand
    {
        [Command("add")]
        [Description("Add a question.")]
        public static async Task AddQuestionAsync(CommandContext context)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await Permissions.Api.Admin.CheckAsync(context, config))
                return;

            if (!await Api.IsWithinMaxQuestionsAmount(context, 1))
                return;

            await (context as SlashCommandContext)!.RespondWithModalAsync(EventHandlers.General.GetQuestionsAddModal(config));
        }
    }
}
