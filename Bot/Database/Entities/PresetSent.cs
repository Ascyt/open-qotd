using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace CustomQotd.Bot.Database.Entities
{
    public class PresetSent
    {
        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public int PresetIndex { get; set; }
    }
}
