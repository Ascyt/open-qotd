using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Processors.SlashCommands;
using CustomQotd.Features;
using DSharpPlus.Commands.Processors.TextCommands.Parsing;
using CustomQotd.Features.Commands;
using CustomQotd.Database;
using Microsoft.EntityFrameworkCore;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity;
using CustomQotd.Features.QotdSending;

namespace CustomQotd
{
    class Program
    {
        public static DiscordClient Client { get; private set; }

        public static async Task Main(string[] args)
        {
            /* When making changes to the database, change the `#if false` to `#if true`, then run:
                dotnet ef migrations add [MIGRATION_NAME] 
                dotnet ef database updatee
            Replace [MIGRATION_NAME] with a name that describes the migration.
            Then set it back to `false` and you're good to go. */
            #if false
            Console.WriteLine("Applying database migrations...");
                return; // The reason that doing this is important, is because otherwise attempting to migrate would start the bot which would run indefinitely
            #endif

            Console.WriteLine("Starting bot...");

            Console.WriteLine("Loading presets...");
            await Presets.LoadPresets();

            string? discordToken = Environment.GetEnvironmentVariable("CUSTOMQOTD_TOKEN");
            if (string.IsNullOrWhiteSpace(discordToken))
            {
                Console.WriteLine("Error: No discord token found. Please provide a token via the CUSTOMQOTD_TOKEN environment variable.");
                Environment.Exit(1);
            }

            DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(discordToken, TextCommandProcessor.RequiredIntents | SlashCommandProcessor.RequiredIntents | DiscordIntents.MessageContents);

            // Use the commands extension
            builder.UseCommands
            (
                // we register our commands here
                extension =>
                {
                    extension.AddCommands([
                        typeof(ConfigCommand), 
                        typeof(QuestionsCommand), 
                        typeof(SuggestCommand), 
                        typeof(SuggestionsCommands), 
                        typeof(PresetsCommand),
                        typeof(TriggerCommand), 
                        typeof(DebugCommand), 
                        typeof(SimpleCommands)]);
                    TextCommandProcessor textCommandProcessor = new(new()
                    {
                        PrefixResolver = new DefaultPrefixResolver(true, "qotd:").ResolvePrefixAsync
                    });

                    // Add text commands with a custom prefix 
                    extension.AddProcessors(textCommandProcessor);

                    extension.CommandErrored += EventHandlers.CommandErrored;
                },
                new CommandsConfiguration()
                {
                    // The default value, however it's shown here for clarity
                    RegisterDefaultCommandProcessors = true,
                    UseDefaultCommandErrorHandler = false
                }
            );
            builder.UseInteractivity(new InteractivityConfiguration()
            {

            });

            builder.ConfigureEventHandlers(b => b
                .HandleGuildMemberAdded((s, e) =>
                {
                    return Task.CompletedTask;
                }));

            DiscordClient client = builder.Build();
            Client = client;

            DiscordActivity status = new("/qotd", DiscordActivityType.ListeningTo);

            // Now we connect and log in.
            await client.ConnectAsync(status, DiscordUserStatus.Online);

            Console.WriteLine("Bot started");

            // Run the CheckTimeLoop in a separate task
            _ = Task.Run(() => QotdTimer.FetchLoopAsync());

            // And now we wait infinitely so that our bot actually stays connected.
            await Task.Delay(-1);
        }
    }
}