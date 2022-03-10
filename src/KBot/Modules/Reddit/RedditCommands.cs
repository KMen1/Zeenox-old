using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Enums;

namespace KBot.Modules.Reddit;

[Group("reddit", "Reddit parancsok")]
public class Reddit : KBotModuleBase
{
    [SlashCommand("fost", "Küld egy random fost-ot az r/FostTalicska subredditről.")]
    public async Task FostAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await RedditService.GetRandomPostAsync("FostTalicska").ConfigureAwait(false);
        var postUrl = "https://reddit.com" + post.Permalink;
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, postUrl, post.ImageUrl).ConfigureAwait(false);
    }

    [SlashCommand("meme", "Küld egy random mémet az r/memes subredditről.")]
    public async Task MemeAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await RedditService.GetRandomPostAsync("memes").ConfigureAwait(false);
        var postUrl = "https://reddit.com" + post.Permalink;
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, postUrl, post.ImageUrl).ConfigureAwait(false);
    }

    [SlashCommand("blursed", "Küld egy random elátkozott képet az r/blursedimages subredditről.")]
    public async Task BlursedAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await RedditService.GetRandomPostAsync("blursedimages").ConfigureAwait(false);
        var postUrl = "https://reddit.com" + post.Permalink;
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, postUrl, post.ImageUrl).ConfigureAwait(false);
    }

    [RequireNsfw]
    [SlashCommand("pussy", "Küld egy random női nemi szervet az r/pussy subredditről.")]
    public async Task PussyAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await RedditService.GetRandomPostAsync("pussy").ConfigureAwait(false);
        var postUrl = "https://reddit.com" + post.Permalink;
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, postUrl, post.ImageUrl).ConfigureAwait(false);
    }

    [RequireNsfw]
    [SlashCommand("boobs", "Küld egy random női mellet az r/boobs subredditről.")]
    public async Task BoobsAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await RedditService.GetRandomPostAsync("boobs").ConfigureAwait(false);
        var postUrl = "https://reddit.com" + post.Permalink;
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, postUrl, post.ImageUrl).ConfigureAwait(false);
    }

    [RequireNsfw]
    [SlashCommand("ass", "Küld egy random popsi képet az r/ass subredditről.")]
    public async Task AssAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var post = await RedditService.GetRandomPostAsync("ass").ConfigureAwait(false);
        var postUrl = "https://reddit.com" + post.Permalink;
        await FollowupWithEmbedAsync(Color.DarkOrange, post.Title, null, postUrl, post.ImageUrl).ConfigureAwait(false);
    }
}