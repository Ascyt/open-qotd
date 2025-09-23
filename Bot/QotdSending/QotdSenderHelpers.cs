using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Database;
using Microsoft.EntityFrameworkCore;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using OpenQotd.Bot.Helpers;
using OpenQotd.Bot.Exceptions;

namespace OpenQotd.Bot.QotdSending
{
    /// <summary>
    /// Data required to send a QOTD to a guild.
    /// </summary>
    internal struct SendQotdData
    {
        public Config config;
        public DiscordGuild guild;

        public DateTime? previousLastSentTimestamp;
        public Notices.Notice? latestAvailableNotice;

        private DiscordChannel? _qotdChannel;

        /// <exception cref="QotdChannelNotFoundException"></exception>
        public async Task<DiscordChannel> GetQotdChannelAsync()
        {
            if (_qotdChannel is not null)
                return _qotdChannel;

            try
            {
                _qotdChannel = await guild.GetChannelAsync(config.QotdChannelId);
            }
            catch (NotFoundException)
            {
                throw new QotdChannelNotFoundException();
            }
            return _qotdChannel;
        }
    }

    /// <summary>
    /// Provides helper methods for managing and sending QOTD messages.
    /// </summary>
    internal class QotdSenderHelpers
    {
        /// <summary>
        /// Used for selecting random questions and presets.
        /// </summary>
        private static readonly Random _random = new();

        /// <summary>
        /// Selects a random accepted question from the database for the specified guild.
        /// </summary>
        public static async Task<Question?> GetRandomQotd(Config config)
        {
            Question[] questions;
            using (AppDbContext dbContext = new())
            {
                questions = await dbContext.Questions
                    .Where(q => q.ConfigId == config.Id && q.Type == QuestionType.Accepted)
                    .ToArrayAsync();
            }

            if (questions.Length == 0)
                return null;

            return questions[_random.Next(questions.Length)];
        }
        /// <summary>
        /// Selects a random preset index from the available presets that has not been marked as used.
        /// </summary>
        /// <remarks>This method ensures that the returned preset index is not already marked as used in
        /// the provided list. If all presets are marked as used, the method will attempt to find an unused preset up to
        /// a predefined limit (time-to-live). If the limit is reached, an exception is thrown to prevent an infinite
        /// loop.</remarks>
        /// <param name="presetSents">A list of <see cref="PresetSent"/> objects, each representing a preset that has already been used.</param>
        /// <returns>An integer representing the index of a randomly selected preset that has not been used.</returns>
        /// <exception cref="Exception">Thrown if the method fails to find an unused preset within a reasonable number of attempts.</exception>
        public static int GetRandomPreset(List<PresetSent> presetSents)
        {
            bool[] isPresetUsed = new bool[Presets.Values.Length];
            foreach (PresetSent ps in presetSents)
            {
                isPresetUsed[ps.PresetIndex] = true;
            }

            int index;
            int timeToLive = 0x1_000_000;
            do
            {
                index = _random.Next(Presets.Values.Length);
                timeToLive--;
            }
            while (isPresetUsed[index] && timeToLive > 0);

            if (timeToLive == 0)
            {
                throw new Exception("Time to live has expired (endless loop?)");
            }

            return index;
        }

        public static void AddSuggestButtonIfEnabled(Config config, DiscordMessageBuilder messageBuilder)
        {
            if (!config.EnableSuggestions)
                return;

            DiscordButtonComponent suggestButton = new(DiscordButtonStyle.Secondary, $"suggest-qotd/{config.ProfileId}", $"Suggest a new {config.QotdTitle ?? "QOTD"}");

            messageBuilder.AddActionRowComponent(suggestButton);
        }

        public static async Task PinMessageIfEnabled(SendQotdData d, DiscordMessage sentMessage)
        {
            if (!d.config.EnableQotdPinMessage)
                return;

            DiscordChannel qotdChannel = await d.GetQotdChannelAsync();

            DiscordMessage? oldSentMessage = null;
            if (d.config.LastQotdMessageId != null)
            {
                try
                {
                    oldSentMessage = await qotdChannel.GetMessageAsync(d.config.LastQotdMessageId.Value);
                }
                catch (NotFoundException)
                {
                    oldSentMessage = null;
                }
            }

            if (oldSentMessage is not null)
            {
                await oldSentMessage.UnpinAsync();
            }

            await sentMessage.PinAsync();
        }

        public static async Task CreateThreadIfEnabled(SendQotdData d, DiscordMessage sentMessage, int? sentQuestionsCount)
        {
            if (!d.config.EnableQotdCreateThread)
                return;

            await sentMessage.CreateThreadAsync($"QOTD{(sentQuestionsCount is null ? "" : $" #{sentQuestionsCount}")} Discussion ({DateTime.UtcNow:yyyy-MM-dd})", DiscordAutoArchiveDuration.Day, reason: "Automatic QOTD thread");
        }

        public static async Task AddPingRoleIfEnabledAndExistent(SendQotdData d, DiscordMessageBuilder builder)
        { 
            if (d.config.QotdPingRoleId == null)
            {
                return;
            }

            DiscordRole? pingRole;
            try
            {
                pingRole = await d.guild.GetRoleAsync(d.config.QotdPingRoleId.Value);
            }
            catch (NotFoundException)
            {
                await (await d.GetQotdChannelAsync()).SendMessageAsync(
                    GenericEmbeds.Warning("QOTD ping role is set, but not found.\n\n" +
                    "*It can be set using `/config set qotd_ping_role [channel]`, or unset using `/config reset qotd_ping_role`.*")
                    );
                return;
            }

            builder.WithContent(pingRole.Mention);
            builder.WithAllowedMention(new RoleMention(pingRole));
        }
    }
}
