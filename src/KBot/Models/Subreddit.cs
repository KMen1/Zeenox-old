// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8618, MA0048, MA0016
using System.Collections.Generic;
using Newtonsoft.Json;

namespace KBot.Models;

public class Subreddit
{
    [JsonProperty("data")] private SubredditData Data { get; set; }
    public List<Post> Posts => Data.Posts;
}

public class SubredditData
{
    [JsonProperty("children")] public List<Post> Posts { get; set; }
}

public class Post
{
    [JsonProperty("data")] private PostData Data { get; set; }

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