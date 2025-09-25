using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using OpenQotd.Bot;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using OpenQotd.Bot.QotdSending;
using OpenQotd.Bot.EventHandlers;
using OpenQotd.Bot.Commands;
using OpenQotd.Bot.UserCommands;
using Microsoft.Extensions.Configuration;

namespace OpenQotd
{
    class Program
    {
        public static DiscordClient Client { get; private set; } = null!;
        public static AppSettings AppSettings { get; private set; } = null!;

        public static async Task Main(string[] args)
        {
            Console.WriteLine($"OpenQOTD - Questions Of The Day done right");
            Console.WriteLine();

            Console.WriteLine("Loading environment variables...");
            DotNetEnv.Env.Load();
            Console.WriteLine("Environment variables loaded.");

            Console.WriteLine("Loading configuration...");
            if (!File.Exists("appsettings.json"))
            {
                File.Copy("appsettings.defaults.json", "appsettings.json");
            }
            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) 
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            AppSettings = new AppSettings();
            config.Bind(AppSettings);
            Console.WriteLine("Configuration loaded.");

            /* When making changes to the database, in appsettings.json change the EnableDbMigrationMode flag to true, then run:
                dotnet ef migrations add [MIGRATION_NAME] 
                dotnet ef database update
            Replace [MIGRATION_NAME] with a name that describes the migration.
            Then set it back to false and you're good to go. */
            if (AppSettings.EnableDbMigrationMode)
            {
                Console.WriteLine("Database migration mode; not starting client");
                return; // The reason that doing this is important, is because otherwise attempting to migrate would start the bot which would run indefinitely
            }

            Console.WriteLine();
            Console.WriteLine($"Version: {AppSettings.Version}");
            Console.WriteLine();

            Console.WriteLine("Loading presets...");
            await Presets.LoadPresetsAsync();
            Console.WriteLine("Presets loaded.");

            string? discordToken = Environment.GetEnvironmentVariable("OPENQOTD_TOKEN");
            if (string.IsNullOrWhiteSpace(discordToken))
            {
				discordToken = Environment.GetEnvironmentVariable("CUSTOMQOTD_TOKEN"); // backwards compatibility
			}
            if (string.IsNullOrWhiteSpace(discordToken))
            {
                Console.WriteLine("Error: No discord token found. Please provide a token via the OPENQOTD_TOKEN environment variable.");
                Environment.Exit(1);
            }
            Console.WriteLine("Token set.");
            Console.WriteLine("Building client...");

            // Create a sharded (https://dsharpplus.github.io/DSharpPlus/articles/beyond_basics/sharding.html) client.
            DiscordClientBuilder builder = DiscordClientBuilder.CreateSharded(discordToken, SlashCommandProcessor.RequiredIntents);

            // Use the commands extension
            builder.UseCommands
            (
                // we register our commands here
                (IServiceProvider provider, CommandsExtension extension) =>
                {
                    extension.AddCommands([
                        typeof(ProfilesCommand),
                        typeof(ConfigCommand),
                        typeof(QuestionsCommand),
                        typeof(SuggestCommand),
                        typeof(SuggestionsCommands),
                        typeof(PresetsCommand),
                        typeof(TriggerCommand),
                        typeof(DebugCommand),
                        typeof(LeaderboardCommand),
                        typeof(TopicCommand),
                        typeof(SimpleCommands),
                        /*typeof(MyQuestionsCommand)*/]);

                    // Text commands disabled because of missing MessageContent intent. It would require an application to Discord.
                    /*TextCommandProcessor textCommandProcessor = new(new()
                    {
                        PrefixResolver = new DefaultPrefixResolver(true, "qotd:").ResolvePrefixAsync
                    });

                    // Add text commands with a custom prefix 
                    extension.AddProcessors(textCommandProcessor);*/

                    extension.CommandErrored += ErrorEventHandlers.CommandErrored;
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
                .HandleComponentInteractionCreated(EventHandlers.ComponentInteractionCreated)
                .HandleModalSubmitted(EventHandlers.ModalSubmittedEvent));

            DiscordClient client = builder.Build();
            Client = client;

            DiscordActivity status = new("/qotd", DiscordActivityType.ListeningTo);

            Console.WriteLine("Connecting client...");
            // Now we connect and log in.
            await client.ConnectAsync(status, DiscordUserStatus.Online);

            Console.WriteLine("Client started.");

            // Run the CheckTimeLoop in a separate task
            _ = Task.Run(() => QotdSenderTimer.FetchLoopAsync());

            // And now we wait infinitely so that our bot actually stays connected.
            await Task.Delay(-1);
        }
    }
}