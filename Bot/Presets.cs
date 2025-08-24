using CustomQotd.Bot.Database.Entities;

namespace CustomQotd.Bot
{
    public static class Presets
    {
        public class PresetBySent(int id, bool sent)
        {
            public int Id { get; set; } = id;
            public bool Sent { get; set; } = sent;
            public string Text { get => Values[Id]; }

            public override string ToString()
                => $"{(Sent ? ":no_entry_sign:" : ":white_check_mark:")} \"**{Text}**\" (ID: `{Id}`)";
        }

        public static string[] Values { get; private set; } = null!;

        public static async Task LoadPresets()
        {
            Values = await File.ReadAllLinesAsync("presets.txt");
            
            Values = Values
                .ToList()
                .Where(v => !string.IsNullOrEmpty(v))
                .ToArray();
        }

        public static List<PresetBySent> GetValuesBySent(IEnumerable<PresetSent> presetSents)
        {
            List<PresetBySent> output = new();

            for (int i = 0; i < Values.Length; i++)
            {
                output.Add(new PresetBySent(i, presetSents.Any(ps => ps.PresetIndex == i)));
            }

            return output;
        }
    }
}
