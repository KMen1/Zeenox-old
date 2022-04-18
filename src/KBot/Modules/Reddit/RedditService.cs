﻿using System;
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

    public async ValueTask<Post> GetRandomPostFromSubredditAsync(string subredditName)
    {
        var url = $"https://www.reddit.com/r/{subredditName}/.json?sort=hot&limit=30";
        var jsonString = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
        if (jsonString.Contains("{\"message\": \"Not Found\", \"error\": 404}")) return null;
        var subreddit = JsonConvert.DeserializeObject<Subreddit>(jsonString);
        if (subreddit is null) return null;
        var random = new Random();
        var randomNumber = random.Next(0, subreddit.Posts.Count - 1);
        var post = subreddit.Posts[randomNumber];

        var imageUrl = post.ImageUrl;
        if (!imageUrl.EndsWith(".jpg") && !imageUrl.EndsWith(".png") && !imageUrl.EndsWith(".gif") &&
            !imageUrl.EndsWith(".jpeg")) post = subreddit.Posts[random.Next(0, subreddit.Posts.Count - 1)];

        return post;
    }
}