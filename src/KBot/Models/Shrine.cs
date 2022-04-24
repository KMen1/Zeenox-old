// ReSharper disable UnusedAutoPropertyAccessor.Global

#pragma warning disable CS8618, MA0048
using Newtonsoft.Json;

namespace KBot.Models;

public partial class Shrines
{
    //[JsonProperty("id")]
    //public long Id { get; set; }

    [JsonProperty("perks")] public ShrinePerk[] Perks { get; set; }

    //[JsonProperty("start")]
    //public long Start { get; set; }

    [JsonProperty("end")] public long End { get; set; }
}

public class ShrinePerk
{
    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("bloodpoints")] public long Bloodpoints { get; set; }

    [JsonProperty("shards")] public long Shards { get; set; }
}

public partial class Shrines
{
    public static Shrines FromJson(string json)
    {
        return JsonConvert.DeserializeObject<Shrines>(json)!;
    }
}