using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace OpenQotd.Core.Helpers
{
    internal static class General
    {
        /// <summary>
        /// Gets a channel from an ID.
        /// </summary>
        /// <returns>The channel, or null if it's not found</returns>
        public static async Task<DiscordChannel?> GetDiscordChannelAsync(ulong id, DiscordGuild? guild = null, ulong? guildId = null, CommandContext? commandContext = null)
        {
            if (guildId is null && commandContext is null && guild is null)
                throw new ArgumentNullException(nameof(guildId));

            try
            {
                if (guild is null)
                {
                    DiscordGuild? actualGuild = (commandContext is not null) ?
                        commandContext.Guild : await Program.Client.GetGuildAsync(guildId!.Value);

                    if (actualGuild is null)
                        return null;

                    return await actualGuild.GetChannelAsync(id);
                }
                return await guild.GetChannelAsync(id);
            }
            catch (NotFoundException)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a channel from an ID.
        /// </summary>
        /// <returns>The channel, or null if it's not found</returns>
        public static async Task<DiscordMessage?> GetDiscordMessageAsync(ulong id, DiscordChannel channel)
        {
            try
            {
                return await channel.GetMessageAsync(id);
            }
            catch (NotFoundException)
            {
                return null;
            }
        }

        /// <summary>
        /// Trim text if it exceeds maxLength or upon the first new-line character, adding an ellipsis if so.
        /// </summary>
        public static string TrimIfNecessary(string text, int maxLength)
        {
            text = text.Trim();

            int newLinePosition = text.IndexOf('\n');
            if (newLinePosition != -1 && newLinePosition < maxLength)
                return text[..newLinePosition] + "…";

            if (text.Length <= maxLength)
                return text;

            return text[..(maxLength - 1)] + "…";
        }

        /// <summary>
        /// Italicize each line of the given text for Discord markdown.
        /// </summary>
        public static string Italicize(string text)
        {
            return string.Join('\n', text!.Split('\n')
                .Select(line => string.IsNullOrEmpty(line) ? "" : $"*{line}*"));
        }

        /// <summary>
        /// Provisorial logging of rate limit exceptions to a file.
        /// </summary>
        public static async Task LogRateLimitExceptionAsync(RateLimitException ex, string contextInfo = "")
        {
            string log = contextInfo != "" ?
                $"Rate limit hit in context \"{contextInfo}\". " :
                $"Rate limit hit.";
            log += $"\n\tCode: {ex.Response!.StatusCode}.\n\tMessage: {ex.Message}\n\tStack Trace: {ex.StackTrace}\n\tResponse Headers:\n\t{ex.Response.Headers}\n\tResponse ";
            
            await Console.Out.WriteLineAsync(log).ConfigureAwait(false);

            if (!File.Exists("ratelimits.log"))
            {
                await File.WriteAllTextAsync("ratelimits.log", log).ConfigureAwait(false);
            }
            else
            {
                await File.AppendAllTextAsync("ratelimits.log", log).ConfigureAwait(false);
            }

                HttpContent? content = ex.Response.Content;
                string responseContent;
                if (content is not null)
                {
                    int timeoutSeconds = 5;
                    try
                    {
                        responseContent = await content.ReadAsStringAsync().WaitAsync(TimeSpan.FromSeconds(timeoutSeconds)).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        try
                        {
                            using MemoryStream memoryStream = new();
                            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(timeoutSeconds));
                            await content.CopyToAsync(memoryStream, cts.Token).ConfigureAwait(false);
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            using StreamReader streamReader = new(memoryStream);
                            responseContent = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            responseContent = "<response read timed out after 5 seconds>";
                        }
                        catch (Exception copyEx)
                        {
                            responseContent = $"<response copy failed: {copyEx.GetType().Name}: {copyEx.Message}>";
                        }
                    }
                    catch (Exception readEx)
                    {
                        responseContent = $"<response read failed: {readEx.GetType().Name}: {readEx.Message}>";
                    }
                }
                else
                {
                    responseContent = string.Empty;
                }

            log = $"Content:\n\t{responseContent}\n\n\n";
            await Console.Out.WriteLineAsync(log).ConfigureAwait(false);
            await File.AppendAllTextAsync("ratelimits.log", log).ConfigureAwait(false);
        }
    }
}
