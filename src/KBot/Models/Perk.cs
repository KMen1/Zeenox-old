// ReSharper disable UnusedAutoPropertyAccessor.Global

#pragma warning disable CS8618, MA0048
using KBot.Modules.DeadByDaylight;
using Newtonsoft.Json;

namespace KBot.Models;

public partial class Perk
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("character")]
    public long CharacterId { get; set; }

    public string CharacterName => DbDService.GetCharacterNameFromId(CharacterId);
}

public partial class Perk
{
    public static Perk FromJson(string json)
    {
        return JsonConvert.DeserializeObject<Perk>(json)!;
    }
}
