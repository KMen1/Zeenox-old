using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace KBot.Modules.DeadByDaylight.Models
{
    public partial class Shrines
    {
        //[JsonProperty("id")]
        //public long Id { get; set; }

        [JsonProperty("perks")]
        public ShrinePerk[] Perks { get; set; }

        //[JsonProperty("start")]
        //public long Start { get; set; }

        [JsonProperty("end")]
        public long End { get; set; }
    }

    public partial class ShrinePerk
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("bloodpoints")]
        public long Bloodpoints { get; set; }

        [JsonProperty("shards")]
        public long Shards { get; set; }
    }

    public partial class Shrines
    {
        public static Shrines FromJson(string json) => JsonConvert.DeserializeObject<Shrines>(json);
    }
}
