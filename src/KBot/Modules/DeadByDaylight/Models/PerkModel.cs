using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace KBot.Modules.DeadByDaylight.Models
{
    public partial class Perk
    {
        [JsonProperty("id")] public string Id { get; set; }

        [JsonProperty("categories")] public string[] Categories { get; set; }

        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("description")] public string Description { get; set; }

        [JsonProperty("role")] public string Role { get; set; }

        [JsonProperty("character")]
        public long CharacterId { get; set; }
        public string CharacterName => DbDService.GetCharacterNameFromId(CharacterId);

        [JsonProperty("tunables")] public float[][] Tunables { get; set; }

        [JsonProperty("modifier")] public string Modifier { get; set; }

        [JsonProperty("teachable")]
        public long Teachable { get; set; }

        [JsonProperty("image")] public string Image { get; set; }
    }

    public partial class Perk
    {
        public static Perk FromJson(string json) => JsonConvert.DeserializeObject<Perk>(json, Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter {DateTimeStyles = DateTimeStyles.AssumeUniversal}
            },
        };
    }
}