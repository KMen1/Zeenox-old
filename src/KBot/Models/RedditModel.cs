using System.Collections.Generic;
using Newtonsoft.Json;

namespace KBot.Models;

public class RedditModel
{
    [JsonProperty("data")] public SubredditData Data { get; set; }
}

public class SubredditData
{
    [JsonProperty("children")] public List<PostObject> Posts { get; set; }
}

public class PostObject
{
    [JsonProperty("data")] private PostData Data { get; set;}

    public string Title => Data.Title;
    public string ImageUrl => Data.Url;
    public string Name => Data.Name;
    private string Permalink => Data.Permalink;
        
    public string PostUrl => "https://reddit.com" + Permalink;
}

public class PostData
{
    [JsonProperty("url")] public string Url { get; set; }
    [JsonProperty("title")] public string Title { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("permalink")] public string Permalink { get; set; }
}