using CustomQotd.Database.Entities;
using CustomQotd.Database;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

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

                            await context.RespondAsync($"`{count}` questions in total");
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
                    }
                    break;
            }

            await context.RespondAsync("Error: Unknown arg");
        }

        private static FileStream CreateTextFileStream(string content)
        {
            string path = "debug_output.json";

            // Write the content to the temporary file
            File.WriteAllText(path, content);

            // Open a FileStream for the temporary file
            FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);

            return fileStream;
        }
    }
}