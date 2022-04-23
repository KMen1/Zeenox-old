using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.Reddit;

[Group("reddit", "Reddit commands")]
public class RedditCommands : SlashModuleBase
{
    private readonly RedditService _redditService;
    public RedditCommands(RedditService redditService)
    {
        _redditService = redditService;
    }

    [SlashCommand("sub", "Sends a random post from the specified subreddit.")]
    public async Task SubAsync(string subreddit)
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await _redditService.GetRandomPostFromSubredditAsync(subreddit).ConfigureAwait(false);
        if (post is null)
        {
            await FollowupWithEmbedAsync(Color.Red, "The subreddit doesn't exist.", "").ConfigureAwait(false);
            return;
        }

        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, post.PostUrl, post.ImageUrl)
            .ConfigureAwait(false);
    }

    [SlashCommand("fost", "Sends a random post from r/FostTalicska.")]
    public async Task FostAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await _redditService.GetRandomPostFromSubredditAsync("FostTalicska").ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, post.PostUrl, post.ImageUrl)
            .ConfigureAwait(false);
    }

    [SlashCommand("meme", "Sends a random post from r/memes.")]
    public async Task MemeAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await _redditService.GetRandomPostFromSubredditAsync("memes").ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, post.PostUrl, post.ImageUrl)
            .ConfigureAwait(false);
    }

    [SlashCommand("blursed", "Sends a random post from r/blursedimages.")]
    public async Task BlursedAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await _redditService.GetRandomPostFromSubredditAsync("blursedimages").ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, post.PostUrl, post.ImageUrl)
            .ConfigureAwait(false);
    }

    [RequireNsfw]
    [SlashCommand("pussy", "Sends a random post from r/pussy.")]
    public async Task PussyAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await _redditService.GetRandomPostFromSubredditAsync("pussy").ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, post.PostUrl, post.ImageUrl)
            .ConfigureAwait(false);
    }

    [RequireNsfw]
    [SlashCommand("boobs", "Sends a random post from r/boobs.")]
    public async Task BoobsAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await _redditService.GetRandomPostFromSubredditAsync("boobs").ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, post.PostUrl, post.ImageUrl)
            .ConfigureAwait(false);
    }

    [RequireNsfw]
    [SlashCommand("ass", "Sends a random post from r/ass.")]
    public async Task AssAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await _redditService.GetRandomPostFromSubredditAsync("ass").ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, post.PostUrl, post.ImageUrl)
            .ConfigureAwait(false);
    }
}