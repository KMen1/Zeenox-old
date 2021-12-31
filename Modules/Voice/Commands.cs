using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Services;

namespace KBot.Modules.Voice;

public class VoiceCommands : InteractionModuleBase<SocketInteractionContext>
{
    public AudioService AudioService { get; set; }
    
    [SlashCommand("join", "Csatlakozik ahhoz a hangcsatornához, amelyben éppen tartózkodsz")]
    public async Task Join()
    {
        await RespondAsync(embed: 
            await AudioService.JoinAsync(Context.Guild, (ITextChannel) Context.Channel, Context.User));
    }

    [SlashCommand("move", "Átlép abba a hangcsatornába, amelyben tartózkodsz")]
    public async Task Move()
    {
        await RespondAsync(embed: await AudioService.MoveAsync(Context.Guild, Context.User));
    }

    [SlashCommand("leave", "Elhagyja azt a hangcsatornát, amelyben a bot éppen tartózkodik")]
    public async Task Leave()
    {
        await RespondAsync(embed: await AudioService.LeaveAsync(Context.Guild, Context.User));
    }

    [SlashCommand("play", "Lejátssza a kívánt zenét")]
    public async Task Play([Summary("query", "Zene linkje vagy címe (YouTube, SoundCloud, Twitch)")] string query)
    {
        var (embed, components, addedToQueue) =
            await AudioService.PlayAsync(
                query,
                Context.Guild,
                (ITextChannel) Context.Channel,
                Context.User,
                Context.Interaction);
        await RespondAsync(embed: embed, components: components);
        if (addedToQueue)
        {
            await Task.Delay(5000);
            await DeleteOriginalResponseAsync();
        }
    }

    [SlashCommand("volume", "Hangerő beállítása")]
    public async Task Volume([Summary("volume", "Hangerő számban megadva (1-100)"), MinValue(1), MaxValue(100)] ushort volume)
    {
        await RespondAsync(embed: await AudioService.SetVolumeAsync(volume, Context.Guild, Context.User), ephemeral: true);
    }

    [SlashCommand("queue", "A sorban lévő zenék listája")]
    public async Task Queue()
    {
        await RespondAsync(embed: await AudioService.GetQueue(Context.Guild, Context.User));
    }

    [SlashCommand("speed", "Zene sebességének növelése")]
    public async Task Speed(
        [Summary("speed", "Sebesség számban megadva (1-10)"), MinValue(1), MaxValue(10)] int speed)
    {
        await RespondAsync(embed: await AudioService.SetSpeedAsync(speed, Context.Guild, Context.User), ephemeral: true);
    }

    [SlashCommand("pitch", "Zene hangmagasságának növelése")]
    public async Task Pitch(
        [Summary("pitch", "Hangmagasság számban megadva (1-10)"), MinValue(1), MaxValue(10)] int pitch)
    {
        await RespondAsync(embed: await AudioService.SetPitchAsync(pitch, Context.Guild, Context.User), ephemeral: true);
    }
    
    [SlashCommand("clearqueue", "A sorban lévő zenék törlése")]
    public async Task ClearQueue()
    {
        await RespondAsync(embed: await AudioService.ClearQueue(Context.Guild, Context.User));
        await Task.Delay(5000);
        await DeleteOriginalResponseAsync();
    }
}