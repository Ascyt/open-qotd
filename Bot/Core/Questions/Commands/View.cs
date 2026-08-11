using System.ComponentModel;
using System.Text;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using OpenQotd.Core.Questions.Entities;

namespace OpenQotd.Core.Questions.Commands
{
    public sealed partial class QuestionsCommand
    {   
        [Command("view")]
        [Description("View a question using its ID.")]
        public static async Task ViewQuestionAsync(CommandContext context,
        [Description("The ID of the question.")] int questionId)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await Permissions.Api.Admin.CheckAsync(context, config))
                return;

            Question? question;
            using (AppDbContext dbContext = new())
            {
                question = await dbContext.Questions
                    .Where(q => q.ConfigId == config.Id && q.GuildDependentId == questionId)
                    .FirstOrDefaultAsync();
            }

            if (question == null)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(title:"Question Not Found", message:$"The question with ID `{questionId}` could not be found."));
                return;
            }

            await context.RespondAsync(GetQuestionsViewResponse(config, question));
        }

        public static DiscordMessageBuilder GetQuestionsViewResponse(Config config, Question question)
        {
            DiscordMessageBuilder response = new();

            StringBuilder generalInfo = new();
            generalInfo.AppendLine($"Belongs to profile: **{config.ProfileName}**");
            generalInfo.AppendLine($"ID: `{question.GuildDependentId}`");
            generalInfo.AppendLine($"Type: {Question.TypeToStyledString(question.Type)}");
            generalInfo.AppendLine();
            generalInfo.AppendLine($"Submitted by: <@{question.SubmittedByUserId}> (`{question.SubmittedByUserId}`)");
            generalInfo.AppendLine($"Submitted at: {DSharpPlus.Formatter.Timestamp(question.Timestamp, DSharpPlus.TimestampFormat.ShortDateTime)}");
            if (question.AcceptedByUserId is not null || question.AcceptedTimestamp is not null)
            {
                generalInfo.AppendLine();
                if (question.AcceptedByUserId is not null)
                    generalInfo.AppendLine($"Accepted by: <@{question.AcceptedByUserId}> (`{question.AcceptedByUserId}`)");
                if (question.AcceptedTimestamp is not null)
                    generalInfo.AppendLine($"Accepted at: {DSharpPlus.Formatter.Timestamp(question.AcceptedTimestamp.Value, DSharpPlus.TimestampFormat.ShortDateTime)}");
            }
            if (question.SentTimestamp is not null || question.SentNumber is not null)
            {
                generalInfo.AppendLine();
                if (question.SentTimestamp is not null)
                    generalInfo.AppendLine($"Sent at: {DSharpPlus.Formatter.Timestamp(question.SentTimestamp.Value, DSharpPlus.TimestampFormat.ShortDateTime)}");
                if (question.SentNumber is not null)
                    generalInfo.AppendLine($"Sent number: **{question.SentNumber}**");
            }
            response.AddEmbed(GenericEmbeds.Info(title: "General", message: generalInfo.ToString()));

            response.AddEmbed(GenericEmbeds.Info(title: "Contents", message: question.Text!).WithFooter($"Written by the submittor. Gets sent as the main {config.QotdShorthandText} body."));

            if (!string.IsNullOrWhiteSpace(question.Notes))
                response.AddEmbed(GenericEmbeds.Info(title: "Additional Notes", message: question.Notes).WithFooter($"Written by the submittor. Gets shown when a button under the {config.QotdShorthandText} is pressed."));

            if (!string.IsNullOrWhiteSpace(question.SuggesterAdminOnlyInfo))
                response.AddEmbed(GenericEmbeds.Info(title: "Admin-Only Info", message: question.SuggesterAdminOnlyInfo).WithFooter("Written by the submittor. Visible to staff only."));

            if (!string.IsNullOrWhiteSpace(question.ThumbnailImageUrl))
                response.AddEmbed(GenericEmbeds.Info(title: "Thumbnail Image", message: $"URL: <{question.ThumbnailImageUrl}>")
                    .WithImageUrl(question.ThumbnailImageUrl)
                    .WithFooter("Thumbnail image URL, as provided by the submittor. Gets shown as a small image above the main body."));

            return response;
        }
    }
}
