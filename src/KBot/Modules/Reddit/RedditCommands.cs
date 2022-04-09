using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.Reddit;

[Group("reddit", "Reddit parancsok")]
public class Reddit : KBotModuleBase
{
    public RedditService RedditService { get; set; }

    [SlashCommand("sub", "Küld egy random post-ot az adott subredditről.")]
    public async Task SubAsync(string subreddit)
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await RedditService.GetRandomPostFromSubredditAsync(subreddit).ConfigureAwait(false);
        if (post is null)
        {
            await FollowupWithEmbedAsync(Color.Red, "Nem található ilyen subreddit.", "").ConfigureAwait(false);
            return;
        }
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, post.PostUrl, post.ImageUrl).ConfigureAwait(false);
    }

    [SlashCommand("fost", "Küld egy random fost-ot az r/FostTalicska subredditről.")]
    public async Task FostAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await RedditService.GetRandomPostFromSubredditAsync("FostTalicska").ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, post.PostUrl, post.ImageUrl).ConfigureAwait(false);
    }

    [SlashCommand("meme", "Küld egy random mémet az r/memes subredditről.")]
    public async Task MemeAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await RedditService.GetRandomPostFromSubredditAsync("memes").ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, post.PostUrl, post.ImageUrl).ConfigureAwait(false);
    }

    [SlashCommand("blursed", "Küld egy random elátkozott képet az r/blursedimages subredditről.")]
    public async Task BlursedAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await RedditService.GetRandomPostFromSubredditAsync("blursedimages").ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, post.PostUrl, post.ImageUrl).ConfigureAwait(false);
    }

    [RequireNsfw]
    [SlashCommand("pussy", "Küld egy random női nemi szervet az r/pussy subredditről.")]
    public async Task PussyAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await RedditService.GetRandomPostFromSubredditAsync("pussy").ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, post.PostUrl, post.ImageUrl).ConfigureAwait(false);
    }

    [RequireNsfw]
    [SlashCommand("boobs", "Küld egy random női mellet az r/boobs subredditről.")]
    public async Task BoobsAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await RedditService.GetRandomPostFromSubredditAsync("boobs").ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, post.PostUrl, post.ImageUrl).ConfigureAwait(false);
    }

    [RequireNsfw]
    [SlashCommand("ass", "Küld egy random popsi képet az r/ass subredditről.")]
    public async Task AssAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await RedditService.GetRandomPostFromSubredditAsync("ass").ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, post.PostUrl, post.ImageUrl).ConfigureAwait(false);
    }
}