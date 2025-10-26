using OpenQotd.Database.Entities;

namespace OpenQotd
{
    /// <summary>
    /// Presets are predefined questions that can be sent as QOTDs. 
    /// </summary>
    /// <remarks>
    /// Questions of type <see cref="QuestionType.Accepted"/> always take priority over presets when sending QOTDs. <br />
    /// Presets are stored in the `presets.txt` file, with one question per line.
    /// </remarks>
    public static class Presets
    {
        /// <summary>
        /// Represents a preset question along with its ID and whether it has been sent in a certain guild.
        /// </summary>
        public class GuildDependentPreset(int id, bool isSent)
        {
            /// <summary>
            /// Equals the index of the preset in the <see cref="values"/> array, which serves as its unique ID.
            /// </summary>
            public int Id { get; set; } = id;

            /// <summary>
            /// Represents whether this preset question has been sent as a QOTD in the guild or has been manually disabled.O
            /// </summary>
            public bool IsSent { get; set; } = isSent;

            /// <summary>
            /// The text of the preset question.
            /// </summary>
            public string Text { get => Values[Id]; }

            public override string ToString()
                => $"{(IsSent ? ":no_entry_sign:" : ":white_check_mark:")} \"**{Text}**\" (ID: `{Id}`)";
        }

        /// <summary>
        /// Represents all global preset questions loaded from the `presets.txt` file.
        /// </summary>
        /// <remarks>
        /// The index of each question in this array serves as its unique ID.
        /// </remarks>
        public static string[] Values { get; private set; } = null!;

        /// <summary>
        /// Load all presets from the `presets.txt` file.
        /// </summary>
        public static async Task LoadPresetsAsync()
        {
            Values = await File.ReadAllLinesAsync("presets.txt");
            
            Values = Values
                .ToList()
                .Where(v => !string.IsNullOrEmpty(v))
                .ToArray();
        }

        /// <summary>
        /// Get all presets along with their sent status for a specific guild, based on the provided set of <see cref="PresetSent"/> entries.
        /// </summary>
        public static List<GuildDependentPreset> GetPresetsAsGuildDependent(HashSet<PresetSent> presetSents)
        {
            List<GuildDependentPreset> output = [];

            for (int i = 0; i < Values.Length; i++)
            {
                output.Add(new GuildDependentPreset(i, presetSents.Any(ps => ps.PresetIndex == i)));
            }

            return output;
        }
    }
}
