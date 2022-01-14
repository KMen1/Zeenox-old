using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Services;

namespace KBot.Modules.Voice;

public class VoiceCommands : InteractionModuleBase<SocketInteractionContext>
{
    public AudioService AudioService { get; set; }

    [SlashCommand("move", "Átlép abba a hangcsatornába, amelyben tartózkodsz")]
    public async Task Move()
    {
        await RespondAsync(embed: await AudioService.MoveAsync(Context.Guild, Context.User));
    }

    [SlashCommand("leave", "Elhagyja azt a hangcsatornát, amelyben a bot éppen tartózkodik")]
    public async Task Leave()
    {
        await RespondAsync(embed: await AudioService.DisconnectAsync(Context.Guild));
    }

    [SlashCommand("play", "Lejátssza a kívánt zenét")]
    public async Task Play([Summary("query", "Zene linkje vagy címe (YouTube, SoundCloud, Twitch)")] string query)
    {
        await DeferAsync();
        await AudioService.PlayAsync(query, Context.Guild, (ITextChannel) Context.Channel, Context.User, Context.Interaction);
    }

    [SlashCommand("volume", "Hangerő beállítása")]
    public async Task Volume([Summary("volume", "Hangerő számban megadva (1-100)"), MinValue(1), MaxValue(100)] ushort volume)
    {
        await RespondAsync(embed: await AudioService.SetVolumeAsync(volume, Context.Guild), ephemeral: true);
    }

    [SlashCommand("queue", "A sorban lévő zenék listája")]
    public async Task Queue()
    {
        await RespondAsync(embed: await AudioService.GetQueue(Context.Guild));
    }

    [SlashCommand("clearqueue", "A sorban lévő zenék törlése")]
    public async Task ClearQueue()
    {
        await RespondAsync(embed: await AudioService.ClearQueue(Context.Guild));
        await Task.Delay(5000);
        await DeleteOriginalResponseAsync();
    }
}