using KBot.Modules.DeadByDaylight;
using Newtonsoft.Json;

namespace KBot.Models;

public partial class Perk
{
    //[JsonProperty("id")] public string Id { get; set; }

    //[JsonProperty("categories")] public string[] Categories { get; set; }

    [JsonProperty("name")] public string Name { get; set; }

    // [JsonProperty("description")] public string Description { get; set; }

    //[JsonProperty("role")] public string Role { get; set; }

    [JsonProperty("character")] public long CharacterId { get; set; }

    public string CharacterName => DbDService.GetCharacterNameFromId(CharacterId);

    // [JsonProperty("tunables")] public float[][] Tunables { get; set; }

    //[JsonProperty("modifier")] public string Modifier { get; set; }

    //[JsonProperty("teachable")] public long Teachable { get; set; }

    //[JsonProperty("image")] public string Image { get; set; }
}

public partial class Perk
{
    public static Perk FromJson(string json)
    {
        return JsonConvert.DeserializeObject<Perk>(json);
    }
}