using System.Threading.Tasks;
using Discord.Interactions;
using KBot.Enums;
using KBot.Helpers;

namespace KBot.Modules.Fun;

public class Reddit : KBotModuleBase
{

    [SlashCommand("fost", "Küld egy random fost-ot az r/FostTalicska subredditről.")]
    public async Task FostAsync()
    {
        await DeferAsync();
        var post = await RedditHelper.GetRandomPost("FostTalicska");
        var title = post.Data.Title;
        var imageUrl = post.Data.Url;
        var postUrl = "https://reddit.com" + post.Data.Permalink;
 
        await FollowupWithEmbedAsync(EmbedResult.Success, title, null, postUrl, imageUrl);
    }

    [SlashCommand("meme", "Küld egy random mémet az r/memes subredditről.")]
    public async Task MemeAsync()
    {
        await DeferAsync();
        var post = await RedditHelper.GetRandomPost("memes");
        var title = post.Data.Title;
        var imageUrl = post.Data.Url;
        var postUrl = "https://reddit.com" + post.Data.Permalink;

        await FollowupWithEmbedAsync(EmbedResult.Success, title, null, postUrl, imageUrl);

    }
    
    
    [SlashCommand("blursed", "Küld egy random elátkozott képet az r/blursedimages subredditről.")]
    public async Task BlursedAsync()
    {
        await DeferAsync();
        var post = await RedditHelper.GetRandomPost("blursedimages");
        var title = post.Data.Title;
        var imageUrl = post.Data.Url;
        var postUrl = "https://reddit.com" + post.Data.Permalink;

        await FollowupWithEmbedAsync(EmbedResult.Success, title, null, postUrl, imageUrl);
    }
    
    [RequireNsfw]
    [SlashCommand("pussy", "Küld egy random női nemi szervet az r/pussy subredditről.")]
    public async Task PussyAsync()
    {
        await DeferAsync();
        var post = await RedditHelper.GetRandomPost("pussy");
        var title = post.Data.Title;
        var imageUrl = post.Data.Url;
        var postUrl = "https://reddit.com" + post.Data.Permalink;

        await FollowupWithEmbedAsync(EmbedResult.Success, title, null, postUrl, imageUrl);

    }
    
    [RequireNsfw]
    [SlashCommand("boobs", "Küld egy random női mellet az r/boobs subredditről.")]
    public async Task BoobsAsync()
    {
        await DeferAsync();
        var post = await RedditHelper.GetRandomPost("boobs");
        var title = post.Data.Title;
        var imageUrl = post.Data.Url;
        var postUrl = "https://reddit.com" + post.Data.Permalink;

        await FollowupWithEmbedAsync(EmbedResult.Success, title, null, postUrl, imageUrl);

    }
    
    [RequireNsfw]
    [SlashCommand("ass", "Küld egy random popsi képet az r/ass subredditről.")]
    public async Task AssAsync()
    {
        await DeferAsync();
        var post = await RedditHelper.GetRandomPost("ass");
        var title = post.Data.Title;
        var imageUrl = post.Data.Url;
        var postUrl = "https://reddit.com" + post.Data.Permalink;

        await FollowupWithEmbedAsync(EmbedResult.Success, title, null, postUrl, imageUrl);
    }
}