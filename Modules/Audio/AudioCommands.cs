using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.Audio;

[Group("music", "Audio parancsok")]
public class MusicCommands : InteractionModuleBase<SocketInteractionContext>
{
    public AudioService AudioService { get; set; }

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
        await AudioService
            .PlayAsync(Context.Guild, (ITextChannel) Context.Channel, Context.User, Context.Interaction, query)
            .ConfigureAwait(false);
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
        return RespondAsync(embed: AudioService.GetQueue(Context.Guild));
    }

    [SlashCommand("clearqueue", "A sorban lévő zenék törlése")]
    public async Task ClearQueue()
    {
        await RespondAsync(embed: await AudioService.ClearQueueAsync(Context.Guild).ConfigureAwait(false)).ConfigureAwait(false);
        await Task.Delay(5000).ConfigureAwait(false);
        await DeleteOriginalResponseAsync().ConfigureAwait(false);
    }
}