using CustomQotd.Bot.Database.Entities;
using CustomQotd.Bot.Database;
using Microsoft.EntityFrameworkCore;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using CustomQotd.Bot.Helpers;

namespace CustomQotd.Bot.QotdSending
{
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


    internal class QotdSenderHelpers
    {
        private static readonly Random _random = new();

        public static async Task<Question?> GetRandomQotd(ulong guildId)
        {
            Question[] questions;
            using (var dbContext = new AppDbContext())
            {
                questions = await dbContext.Questions
                    .Where(q => q.GuildId == guildId && q.Type == QuestionType.Accepted)
                    .ToArrayAsync();
            }

            if (questions.Length == 0)
                return null;

            return questions[_random.Next(questions.Length)];
        }
        public static int GetRandomPreset(List<PresetSent> presetSents)
        {
            bool[] isPresetUsed = new bool[Presets.Values.Length];
            foreach (var presetSent in presetSents)
            {
                isPresetUsed[presetSent.PresetIndex] = true;
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

            DiscordButtonComponent suggestButton = new(DiscordButtonStyle.Secondary, "suggest-qotd", "Suggest a new QOTD");

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
            DiscordRole? pingRole = null;
            if (d.config.QotdPingRoleId == null)
            {
                return;
            }

            try
            {
                pingRole = await d.guild.GetRoleAsync(d.config.QotdPingRoleId.Value);
            }
            catch (NotFoundException)
            {
                await (await d.GetQotdChannelAsync()).SendMessageAsync(
                    MessageHelpers.GenericWarningEmbed("QOTD ping role is set, but not found.\n\n" +
                    "*It can be set using `/config set qotd_ping_role [channel]`, or unset using `/config reset qotd_ping_role`.*")
                    );
                return;
            }

            builder.WithContent(pingRole.Mention);
            builder.WithAllowedMention(new RoleMention(pingRole));
        }
    }
}
