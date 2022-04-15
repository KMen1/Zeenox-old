using System;
using System.Net.Http;
using System.Threading.Tasks;
using KBot.Models;
using Newtonsoft.Json;

namespace KBot.Modules.Reddit;

public class RedditService : IInjectable
{
    private readonly HttpClient _httpClient;

    public RedditService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async ValueTask<PostObject> GetRandomPostFromSubredditAsync(string subreddit)
    {
        var url = $"https://www.reddit.com/r/{subreddit}/.json?sort=hot&limit=30";
        var jsonString = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
        if (jsonString.Contains("{\"message\": \"Not Found\", \"error\": 404}")) return null;
        var subredditObject = JsonConvert.DeserializeObject<RedditModel>(jsonString);
        if (subredditObject is null) return null;
        var random = new Random();
        var randomNumber = random.Next(0, subredditObject.Data.Posts.Count);
        var post = subredditObject.Data.Posts[randomNumber];

        var imageUrl = post.ImageUrl;
        if (!imageUrl.EndsWith(".jpg") && !imageUrl.EndsWith(".png") && !imageUrl.EndsWith(".gif") &&
            !imageUrl.EndsWith(".jpeg")) post = await GetRandomPostFromSubredditAsync(subreddit).ConfigureAwait(false);

        return post;
    }
}