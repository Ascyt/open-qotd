﻿using CustomQotd.Database.Entities;
using CustomQotd.Database;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;
using Microsoft.AspNetCore.Http.Connections;
using DSharpPlus.Interactivity.Extensions;
using System.Threading.Channels;

namespace CustomQotd.Features.Commands
{
    public class DebugCommand
    {
        public static HashSet<ulong> allowedUsers = new()
        {
            417669404537520128
        };

        [Command("debug")]
        [Description("Debug command that can only be executed by developers of CustomQOTD.")]
        public static async Task DebugAsync(CommandContext context, [Description("Debug arguments")] string args)
        {
            if (!allowedUsers.Contains(context.User.Id))
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed("This command can only be executed by developers of CustomQOTD.")
                    );
                return;
            }

            string[] argsSplit = args.Split(' ');

            switch (argsSplit[0])
            {
                case "c":
                    switch (argsSplit[1])
                    {
                        case "count":
                            int count;
                            using (var dbContext = new AppDbContext())
                            {
                                count = await dbContext.Configs.CountAsync();
                            }

                            await context.RespondAsync($"`{count}` initialized configs");
                            return;

                        case "getall":
                            string data;
                            using (var dbContext = new AppDbContext())
                            {
                                data = JsonConvert.SerializeObject(await dbContext.Configs.ToArrayAsync(), Formatting.Indented);
                            }
                            DiscordMessageBuilder builder = new();

                            using (FileStream fileStream = CreateTextFileStream(data))
                            {
                                builder.AddFile(fileStream);

                                await context.RespondAsync(builder);

                                fileStream.Close();
                            }
                            return;

                        case "get":
                            ulong guildId = argsSplit.Length > 2 ? ulong.Parse(argsSplit[2]) : context.Guild!.Id;

                            string guildSpecificData;
                            using (var dbContext = new AppDbContext())
                            {
                                guildSpecificData = JsonConvert.SerializeObject(await dbContext.Configs.Where(c => c.GuildId == guildId).ToArrayAsync(), Formatting.Indented);
                            }
                            DiscordMessageBuilder builder1 = new();

                            using (FileStream fileStream = CreateTextFileStream(guildSpecificData))
                            {
                                builder1.AddFile(fileStream);

                                await context.RespondAsync(builder1);

                                fileStream.Close();
                            }
                            return;

                        case "resetlastsentday":
                            ulong guildId1 = argsSplit.Length > 2 ? ulong.Parse(argsSplit[2]) : context.Guild!.Id;

                            using (var dbContext = new AppDbContext())
                            {
                                Config? config = await dbContext.Configs.Where(c => c.GuildId == guildId1).FirstOrDefaultAsync();

                                if (config == null)
                                {
                                    await context.RespondAsync("Guild not found");
                                    return;
                                }

                                config.LastSentDay = -1;
                                await dbContext.SaveChangesAsync();
                            }

                            await context.RespondAsync("Resetted last_sent_day");
                            return;
                    }
                    break;
                case "q":
                    switch (argsSplit[1])
                    {
                        case "count":
                            int count;
                            using (var dbContext = new AppDbContext())
                            {
                                count = await dbContext.Questions.CountAsync();
                            }

                            await context.RespondAsync($"`{count}` questions in total.");
                            return;

                        case "getall":
                            string data;
                            using (var dbContext = new AppDbContext())
                            {
                                data = JsonConvert.SerializeObject(await dbContext.Questions.ToArrayAsync(), Formatting.Indented);
                            }
                            DiscordMessageBuilder builder = new();

                            using (FileStream fileStream = CreateTextFileStream(data))
                            {
                                builder.AddFile(fileStream);

                                await context.RespondAsync(builder);

                                fileStream.Close();
                            }
                            return;

                        case "get":
                            ulong guildId = argsSplit.Length > 2 ? ulong.Parse(argsSplit[2]) : context.Guild!.Id;

                            string guildSpecificData;
                            using (var dbContext = new AppDbContext())
                            {
                                guildSpecificData = JsonConvert.SerializeObject(await dbContext.Questions.Where(c => c.GuildId == guildId).ToArrayAsync(), Formatting.Indented);
                            }
                            DiscordMessageBuilder builder1 = new();
                            using (FileStream fileStream = CreateTextFileStream(guildSpecificData))
                            {
                                builder1.AddFile(fileStream);

                                await context.RespondAsync(builder1);

                                fileStream.Close();
                            }
                            return;

                        case "add":
                            await AddQuestionsAsync(context, argsSplit);
                            return;

                        case "removeduplicates":
                            List<Question> questions;
                            using (var dbContext = new AppDbContext())
                            {
                                questions = await dbContext.Questions.Where(c => c.GuildId == context.Guild!.Id).ToListAsync();
                            }

                            List<int> duplicateIds = questions
                                .GroupBy(q => q.Text)
                                .Where(g => g.Count() > 1)
                                .SelectMany(g => g.Skip(1).Select(q => q.Id))
                                .ToList();

                            using (var dbContext = new AppDbContext())
                            {
                                foreach (int id in duplicateIds)
                                {
                                    Question? question = await dbContext.Questions.Where(q => q.Id == id).FirstOrDefaultAsync();

                                    if (question == null)
                                        continue;

                                    dbContext.Remove(question);
                                }

                                await dbContext.SaveChangesAsync();
                            }

                            await context.RespondAsync($"Removed {duplicateIds.Count} duplicates.");

                            return;
                    }
                    break;
                case "f":
                    switch (argsSplit[1])
                    {
                        case "get":
                            DiscordMessageBuilder builder1 = new();
                            using (FileStream fileStream = CreateTextFileStream(await File.ReadAllTextAsync("feedback.txt"), "debug_output.txt"))
                            {
                                builder1.AddFile(fileStream);

                                await context.RespondAsync(builder1);

                                fileStream.Close();
                            }
                            return;
                        case "reset":
                            await File.WriteAllTextAsync("feedback.txt", "");
                            await context.RespondAsync("Successfully resetted feedback.");
                            return;
                    }
                    break;
            }
        }

        private static async Task AddQuestionsAsync(CommandContext context, string[] argsSplit)
        {
            QuestionType type;
            switch (argsSplit[2].ToLower())
            {
                case "suggested":
                    type = QuestionType.Suggested;
                    break;
                case "accepted":
                    type = QuestionType.Accepted;
                    break;
                case "sent":
                    type = QuestionType.Sent;
                    break;
                default:
                    await context.RespondAsync("Error: Unknown question type");
                    return;
            }

            await context.RespondAsync($"Adding questions of type {type}.\n\nFormat:\n`{{userId}} {{question}}`\n\nBackslash (`\\`) to cancel.");

            DiscordMessage message = await context.Channel!.SendMessageAsync($"Now intercepting messages from {context.User.Mention}");

            while (true)
            {
                StringBuilder response = new StringBuilder();

                var result = await message.Channel!.GetNextMessageAsync(m =>
                {
                    return (m.Author!.Id == context.User.Id);
                });

                if (result.TimedOut || result.Result == null)
                {
                    await context.Channel.SendMessageAsync("Interception stopped because of timeout.");
                    return;
                }

                string[] results = result.Result.Content.Split('\n');
                foreach (string s in results)
                {
                    if (s == "\\")
                    {
                        await context.Channel.SendMessageAsync("Interception stopped.");
                        return;
                    }

                    string userIdString = s.Split(' ')[0];
                    string questionText = s.Substring(userIdString.Length + 1);
                    ulong userId = ulong.Parse(userIdString);

                    Question newQuestion;

                    using (var dbContext = new AppDbContext())
                    {
                        newQuestion = new Question()
                        {
                            GuildId = context.Guild!.Id,
                            GuildDependentId = await Question.GetNextGuildDependentId(context.Guild!.Id),
                            Type = type,
                            Text = questionText,
                            SubmittedByUserId = userId,
                            Timestamp = DateTime.MinValue
                        };
                        await dbContext.Questions.AddAsync(newQuestion);
                        await dbContext.SaveChangesAsync();
                    }

                    response.AppendLine(newQuestion.ToString());
                }

                await context.Channel!.SendMessageAsync(MessageHelpers.GenericEmbed(title: "Added Questions", message: response.ToString()));
            }
        }

        private static FileStream CreateTextFileStream(string content, string path="debug_output.json")
        {
            // Write the content to the temporary file
            File.WriteAllText(path, content);

            // Open a FileStream for the temporary file
            FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);

            return fileStream;
        }
    }
}