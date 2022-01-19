using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KBot.Helpers;

public static class RedditHelper
{
    public class SubredditObject
    {
        [JsonProperty("data")] public SubredditData Data { get; set; }
    }
    
    public class SubredditData
    {
        [JsonProperty("children")] public IList<PostObject> Posts { get; set; }
    }

    public class PostObject
    {
        [JsonProperty("data")] public PostData Data { get; set; }   
    }

    public class PostData
    {
        [JsonProperty("url")] public string Url { get; set; }
        [JsonProperty("title")] public string Title { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("permalink")] public string Permalink { get; set; }
    }
    
    
    public static async Task<PostObject> GetRandomPost(string subreddit)
    {
        var url = $"https://www.reddit.com/r/{subreddit}/.json?sort=hot&limit=30";
        using var webClient = new HttpClient();
        var jsonString = await webClient.GetStringAsync(url);
        var subredditObject = JsonConvert.DeserializeObject<SubredditObject>(jsonString);

        var random = new Random();
        var randomNumber = random.Next(0, subredditObject.Data.Posts.Count);
        var post = subredditObject.Data.Posts[randomNumber];
        
        var imageUrl = post.Data.Url;
        if (!imageUrl.EndsWith(".jpg") && !imageUrl.EndsWith(".png") && !imageUrl.EndsWith(".gif") && !imageUrl.EndsWith(".jpeg"))
        {
            post = await GetRandomPost(subreddit);
        }
        
        return post;
    }
}