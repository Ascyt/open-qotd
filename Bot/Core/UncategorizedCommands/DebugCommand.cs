using DSharpPlus.Commands;
using DSharpPlus.Entities;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using DSharpPlus.Commands.Processors.SlashCommands;
using OpenQotd.Core.Helpers;
using OpenQotd.Core.Database;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Questions.Entities;

namespace OpenQotd.Core.UncategorizedCommands
{
    public sealed class DebugCommand
    {
        public static readonly HashSet<ulong> sudoUserIds = [];

        [Command("debug")]
        [Description("Debug command that can only be executed by developers of OpenQOTD.")]
        public static async Task DebugAsync(CommandContext context, [Description("Debug arguments")] string args)
        {
            bool isAllowed;
            lock (Program.AppSettings.DebugAllowedUserIds)
            {
                isAllowed = Program.AppSettings.DebugAllowedUserIds.Contains(context.User.Id);
            }
            if (!isAllowed)
            {
                await (context as SlashCommandContext)!.RespondAsync(
                    GenericEmbeds.Error("This command can only be executed by developers of OpenQOTD."),
                    ephemeral: true);
                return;
            }

            await context.DeferResponseAsync();

            string[] argsSplit = args.Split(' ');

            switch (argsSplit[0])
            {
                case "sudo":
                    string? firstArgSudo = argsSplit.Length <= 1 ? null : argsSplit[1];
                    switch (firstArgSudo)
                    {
                        case "0":
                            sudoUserIds.Remove(context.User.Id);
                            await context.RespondAsync($"Sudo disabled.");
                            break;
                        case "1":
                            sudoUserIds.Add(context.User.Id);
                            await context.RespondAsync($"Sudo enabled.");
                            break;
                        default:
                            if (sudoUserIds.Contains(context.User.Id))
                            {
                                sudoUserIds.Remove(context.User.Id);
                                await context.RespondAsync($"Sudo disabled.");
                            }
                            else
                            {
                                sudoUserIds.Add(context.User.Id);
                                await context.RespondAsync($"Sudo enabled.");
                            }
                            break;
                    }
                    return;
                case "c":
                    switch (argsSplit[1])
                    {
                        case "count":
                            int count;
                            using (AppDbContext dbContext = new())
                            {
                                count = await dbContext.Configs.CountAsync();
                            }

                            await context.RespondAsync($"`{count}` initialized configs");
                            return;

                        case "getall":
                            string data;
                            using (AppDbContext dbContext = new())
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
                            using (AppDbContext dbContext = new())
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

                        case "resetlastsenttimestamp":
                            ulong guildId1 = argsSplit.Length > 2 ? ulong.Parse(argsSplit[2]) : context.Guild!.Id;

                            using (AppDbContext dbContext = new())
                            {
                                Config? config = await dbContext.Configs.Where(c => c.GuildId == guildId1).FirstOrDefaultAsync();

                                if (config == null)
                                {
                                    await context.RespondAsync("Guild not found");
                                    return;
                                }

                                config.LastSentTimestamp = null;
                                await dbContext.SaveChangesAsync();
                            }

                            await context.RespondAsync("Resetted last_sent_timestamp");
                            return;
                    }
                    break;
                case "q":
                    switch (argsSplit[1])
                    {
                        case "count":
                            int count;
                            using (AppDbContext dbContext = new())
                            {
                                count = await dbContext.Questions.CountAsync();
                            }

                            await context.RespondAsync($"`{count}` questions in total.");
                            return;

                        case "getall":
                            string data;
                            using (AppDbContext dbContext = new())
                            {
                                dbContext.RemoveRange(
                                    new Question() { Id = 34 },
                                    new Question() { Id = 35 },
                                    new Question() { Id = 36 },
                                    new Question() { Id = 37 }
                                    );

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

                            await context.RespondAsync("Not implemented with profiles");

                            //string guildSpecificData;
                            //using (AppDbContext dbContext = new())
                            //{
                            //    guildSpecificData = JsonConvert.SerializeObject(await dbContext.Questions.Where(c => c.ConfigId == guildId).ToArrayAsync(), Formatting.Indented);
                            //}
                            //DiscordMessageBuilder builder1 = new();
                            //using (FileStream fileStream = CreateTextFileStream(guildSpecificData))
                            //{
                            //    builder1.AddFile(fileStream);

                            //    await context.RespondAsync(builder1);

                            //    fileStream.Close();
                            //}
                            return;

                        case "removeduplicates":
                            await context.RespondAsync("Not implemented with profiles");

                            List<Question> questions;
                            using (AppDbContext dbContext = new())
                            {
                                questions = await dbContext.Questions.Where(c => c.GuildId == context.Guild!.Id).ToListAsync();
                            }

                            List<int> duplicateIds = questions
                                .GroupBy(q => q.Text)
                                .Where(g => g.Count() > 1)
                                .SelectMany(g => g.Skip(1).Select(q => q.Id))
                                .ToList();

                            using (AppDbContext dbContext = new())
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
                case "n":
                    if (argsSplit[1] == "get")
                    {
                        using (FileStream fileStream = CreateTextFileStream(await File.ReadAllTextAsync("notices.json"), "debug_output.txt"))
                        {
                            DiscordMessageBuilder builder2 = new();

                            builder2.AddFile(fileStream);

                            await context.RespondAsync(builder2);

                            fileStream.Close();
                        }
                        return;
                    }
                    if (argsSplit[1] == "reload" || argsSplit[1] == "load")
                    {
                        await Notices.Api.LoadNoticesAsync();
                        await context.RespondAsync("Successfully reloaded notices.");
                        return;
                    }

                    (ulong messageId, string date, bool isImportant) = (ulong.Parse(argsSplit[1]), argsSplit[2], argsSplit[3] == "1");

                    DiscordMessage message = await context.Channel.GetMessageAsync(messageId);

                    Notices.Api.Notice newNotice = new() { Date = DateTime.Parse(date), NoticeText = message.Content, IsImportant = isImportant };

                    Notices.Api.notices.Add(newNotice);

                    await Notices.Api.SaveNoticesAsync();

                    await context.RespondAsync("Successfully added new notice.");
                    return;
                case "throwexception":
                    throw new Exception("Thrown by debug");
                case "sql":
                    if (argsSplit.Length < 2)
                    {
                        await context.RespondAsync("No SQL query provided.");
                        return;
                    }

                    bool isGlobal = argsSplit[1] == "-g";
                    string sqlQuery;
                    if (isGlobal)
                    {
                        if (argsSplit.Length < 3)
                        {
                            await context.RespondAsync("No SQL query provided.");
                            return;
                        }

                        sqlQuery = string.Join(' ', argsSplit.Skip(2));
                    }
                    else
                    {
                        sqlQuery = string.Join(' ', argsSplit.Skip(1));

                        if (!sqlQuery.Contains(context.Guild!.Id.ToString()))
                        {
                            await context.RespondAsync($"The SQL query must contain the guild ID `{context.Guild.Id}`. Run with `-g` to bypass.");
                            return;
                        }
                    }

                    using (AppDbContext dbContext = new())
                    {
                        try
                        {
                            int result = await dbContext.Database.ExecuteSqlRawAsync(sqlQuery);
                            await context.RespondAsync($"SQL query executed successfully. Rows affected: {result}");
                        }
                        catch (Exception ex)
                        {
                            await context.RespondAsync($"Error executing SQL query:\n```{ex.Message}```");
                        }
                    }
                    return;
            }

            await context.RespondAsync("Unknown debug arg");
        }

        private static FileStream CreateTextFileStream(string content, string path="debug_output.json")
        {
            // Write the content to the temporary file
            File.WriteAllText(path, content);

            // Open a FileStream for the temporary file
            FileStream fileStream = new(path, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);

            return fileStream;
        }
    }
}
