using DSharpPlus.Commands;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using OpenQotd.Core.Questions.Entities;
using System.ComponentModel;

namespace OpenQotd.Core.UncategorizedCommands
{
    public class TriggerCommand
    {
        [Command("trigger")]
        [Description("Trigger a QOTD prematurely.")]
        public static async Task TriggerAsync(CommandContext context,
        [Description("Optionally specify the ID of the question to be sent.")] int? questionId = null)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await Permissions.Api.Admin.CheckAsync(context, config))
                return;

            await context.DeferResponseAsync();

            if (questionId is null)
                await QotdSending.Sender.Api.SendRandomQotdAsync(context.Guild!, config, Notices.Api.GetLatestAvailableNotice());
            else
            {
                Question? question;
                using (AppDbContext dbContext = new())
                {
                    question = await dbContext.Questions
                        .Where(q => q.ConfigId == config.Id && q.GuildDependentId == questionId)
                        .FirstOrDefaultAsync();

                    if (question == null)
                    {
                        await context.RespondAsync(
                            GenericEmbeds.Error(title:"Question Not Found", message:$"The question with ID `{questionId}` could not be found."));
                        return;
                    }
                }

                await QotdSending.Sender.Api.SendQotdAsync(context.Guild!, config, question, Notices.Api.GetLatestAvailableNotice());
            }

            await context.RespondAsync(
                GenericEmbeds.Success(title:$"Successfully triggered {config.QotdShorthandText}", 
                message:$"A new {config.QotdTitleText} has been successfully sent to the <#{config.QotdChannelId}> channel."));

            await Logging.Api.LogUserActionAsync(context, config, "Trigger QOTD");
        }
    }
}
