using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Enums;
using KBot.Modules.Audio.Helpers;

namespace KBot.Modules.Audio;

[Group("music", "Audio parancsok")]
public class MusicCommands : KBotModuleBase
{
    [SlashCommand("move", "Átlép abba a hangcsatornába, amelyben tartózkodsz")]
    public async Task Move()
    {
        await RespondAsync(embed: await AudioService.MoveAsync(Context.Guild, Context.User).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    [SlashCommand("leave", "Elhagyja azt a hangcsatornát, amelyben a bot éppen tartózkodik")]
    public async Task Leave()
    {
        await RespondAsync(embed: await AudioService.DisconnectAsync(Context.Guild).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    [SlashCommand("play", "Lejátssza a kívánt zenét")]
    public async Task Play([Summary("query", "Zene linkje vagy címe (YouTube, SoundCloud, Twitch)")] string query)
    {
        await DeferAsync().ConfigureAwait(false);
        if (((IVoiceState)Context.User).VoiceChannel is null)
        {
            await FollowupAsync(embed: Embeds.ErrorEmbed("Nem vagy hangcsatornában!")).ConfigureAwait(false);
            return;
        }
        await AudioService
            .PlayAsync(Context.Guild, Context.Interaction, query)
            .ConfigureAwait(false);
    }

    [SlashCommand("search", "Keres egy zenét a YouTube-on")]
    public async Task Search([Summary("query", "Zene címe")] string query)
    {
        await DeferAsync().ConfigureAwait(false);
        var search = await AudioService.SearchAsync(query).ConfigureAwait(false);
        if (search is null)
        {
            await FollowupWithEmbedAsync(EmbedResult.Error, "Nincs találat!", "").ConfigureAwait(false);
            return;
        }
        var tracks = search.Value.Tracks.ToList();
        var desc = new StringBuilder();
        foreach (var track in tracks.Take(10))
        {
            desc.AppendLine(
                $"{tracks.TakeWhile(n => n != track).Count() + 1}. [`{track.Title}`]({track.Url}) | [`{track.Duration}`]");
        }

        var comp = new ComponentBuilder()
            .WithButton(" ", "0", emote: new Emoji("1️⃣"))
            .WithButton(" ", "1", emote: new Emoji("2️⃣"))
            .WithButton(" ", "2", emote: new Emoji("3️⃣"))
            .WithButton(" ", "3", emote: new Emoji("4️⃣"))
            .WithButton(" ", "4", emote: new Emoji("5️⃣"))
            .WithButton(" ", "5", emote: new Emoji("6️⃣"))
            .WithButton(" ", "6", emote: new Emoji("7️⃣"))
            .WithButton(" ", "7", emote: new Emoji("8️⃣"))
            .WithButton(" ", "8", emote: new Emoji("9️⃣"))
            .WithButton(" ", "9", emote: new Emoji("🔟"))
            .Build();

        var eb = new EmbedBuilder()
            .WithTitle("Válaszd ki a kívánt számot")
            .WithColor(Color.Blue)
            .WithDescription(desc.ToString())
            .Build();

        await FollowupAsync(embed: eb, components: comp).ConfigureAwait(false);

        var result = await InteractiveService.NextMessageComponentAsync(x => x.User.Id == Context.User.Id).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return;
        }

        await result.Value!.DeferAsync().ConfigureAwait(false);
        var index = int.Parse(result.Value!.Data.CustomId);
        await AudioService.PlayAsync(Context.Guild, Context.Interaction, tracks[index]).ConfigureAwait(false);
    }

    [SlashCommand("volume", "Hangerő beállítása")]
    public async Task Volume(
        [Summary("volume", "Hangerő számban megadva (1-100)"), MinValue(1), MaxValue(100)] ushort volume)
    {
        await RespondAsync(embed: await AudioService.SetVolumeAsync(Context.Guild, volume).ConfigureAwait(false),
            ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("queue", "A sorban lévő zenék listája")]
    public Task Queue()
    {
        return RespondAsync(embed: AudioService.GetQueue(Context.Guild), ephemeral: true);
    }

    [SlashCommand("clearqueue", "A sorban lévő zenék törlése")]
    public async Task ClearQueue()
    {
        await RespondAsync(embed: await AudioService.ClearQueueAsync(Context.Guild).ConfigureAwait(false)).ConfigureAwait(false);
        await Task.Delay(5000).ConfigureAwait(false);
        await DeleteOriginalResponseAsync().ConfigureAwait(false);
    }
}