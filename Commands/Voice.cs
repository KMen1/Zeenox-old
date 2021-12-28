using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Services;

namespace KBot.Commands;

public class Voice : InteractionModuleBase<InteractionContext>
{
    public AudioService AudioService { get; set; }
    
    [SlashCommand("join", "Csatlakozik ahhoz a hangcsatornához, amelyben éppen tartózkodsz")]
    public async Task Join()
    {
        await RespondAsync(embed:
            await AudioService.JoinAsync(
                ((ITextChannel) Context.Channel).Guild,
                ((IVoiceState) Context.User).VoiceChannel,
                (ITextChannel) Context.Channel,
                (SocketUser) Context.User));
    }

    [SlashCommand("move", "Átlép abba a hangcsatornába, amelyben tartózkodsz")]
    public async Task Move()
    {
        await RespondAsync(embed:
            await AudioService.MoveAsync(
                ((ITextChannel) Context.Channel).Guild,
                ((IVoiceState) Context.User).VoiceChannel,
                (SocketUser) Context.User));
    }

    [SlashCommand("leave", "Elhagyja azt a hangcsatornát, amelyben a bot éppen tartózkodik")]
    public async Task Leave()
    {
        await RespondAsync(embed:
            await AudioService.LeaveAsync(
                ((ITextChannel) Context.Channel).Guild,
                ((IVoiceState) Context.User).VoiceChannel,
                (SocketUser) Context.User));
    }

    [SlashCommand("play", "Lejátssza a kívánt zenét")]
    public async Task Play([Summary("query", "Zene linkje vagy címe (YouTube, SoundCloud, Twitch)")] string query)
    {
        var (embed, buttons, addedToQueue) =
            await AudioService.PlayAsync(
                query,
                ((ITextChannel) Context.Channel).Guild,
                ((IVoiceState) Context.User).VoiceChannel,
                (ITextChannel) Context.Channel,
                (SocketUser) Context.User,
                Context);
        await RespondAsync(embed: embed, components: buttons);
        if (addedToQueue)
        {
            await Task.Delay(5000);
            await DeleteOriginalResponseAsync();
        }
    }

    /*[SlashCommand("stop", "Zenelejátszás megállítása")]
    public async Task Stop()
    {
        await RespondAsync(embed: await AudioService.StopAsync(((ITextChannel) Context.Channel).Guild,
            (SocketUser) Context.User));
    }*/

    /*[SlashCommand("skip", "Lejátsza a következő zenét a sorban")]
    public async Task Skip()
    {
        var (embed, _) =
            await AudioService.SkipAsync(((ITextChannel) Context.Channel).Guild, (SocketUser) Context.User, false);
        await RespondAsync(embed: embed);
    }*/
    
    /*[SlashCommand("pause", "Zenelejátszás szüneteltetése")]
    public async Task Pause()
    {
        var (embed, _) =
            await AudioService.PauseOrResumeAsync(((ITextChannel) Context.Channel).Guild, (SocketUser) Context.User);
        await RespondAsync(embed: embed);
    }*/

    /*[SlashCommand("resume", "Zenelejátszás folytatása")]
    public async Task Resume()
    {
        var (embed, _) =
            await AudioService.PauseOrResumeAsync(((ITextChannel) Context.Channel).Guild, (SocketUser) Context.User);
        await RespondAsync(embed: embed);
    }*/

    [SlashCommand("volume", "Hangerő beállítása")]
    public async Task Volume([Summary("volume", "Hangerő számban megadva (1-100)"), MinValue(1), MaxValue(100)] ushort volume)
    {
        await RespondAsync(embed: await AudioService.SetVolumeAsync(volume, ((ITextChannel) Context.Channel).Guild,
            (SocketUser) Context.User));
    }

    /*[SlashCommand("loop", "Zene ismétlésének bekapcsolása / kikapcsolása")]
    public async Task Loop()
    {
        await RespondAsync(embed: await AudioService.SetRepeatAsync(((ITextChannel) Context.Channel).Guild,
            (SocketUser) Context.User));
    }*/

    [SlashCommand("queue", "A sorban lévő zenék listája")]
    public async Task Queue()
    {
        await RespondAsync(embed: await AudioService.GetQueue(((ITextChannel) Context.Channel).Guild,
            (SocketUser) Context.User));
    }

    [SlashCommand("bassboost", "Basszus erősítés bekapcsolása")]
    public async Task BassBoost()
    {
        await RespondAsync(embed: await AudioService.SetBassBoostAsync(((ITextChannel) Context.Channel).Guild,
            (SocketUser) Context.User));
        await Task.Delay(5000);
        await DeleteOriginalResponseAsync();
    }

    [SlashCommand("nightcore", "Nightcore mód bekapcsolása")]
    public async Task NightCore()
    {
        await RespondAsync(embed: await AudioService.SetNightCoreAsync(((ITextChannel) Context.Channel).Guild,
            (SocketUser) Context.User));
        await Task.Delay(5000);
        await DeleteOriginalResponseAsync();
    }

    [SlashCommand("8d", "8D mód bekapcsolása")]
    public async Task EightD()
    {
        await RespondAsync(embed: await AudioService.SetEightDAsync(((ITextChannel) Context.Channel).Guild,
            (SocketUser) Context.User));
        await Task.Delay(5000);
        await DeleteOriginalResponseAsync();
    }

    [SlashCommand("vaporwave", "Vaporwave mód bekapcsolása")]
    public async Task VaporWave()
    {
        await RespondAsync(embed: await AudioService.SetVaporWaveAsync(((ITextChannel) Context.Channel).Guild,
            (SocketUser) Context.User));
        await Task.Delay(5000);
        await DeleteOriginalResponseAsync();
    }

    [SlashCommand("speed", "Zene sebességének növelése")]
    public async Task Speed(
        [Summary("speed", "Sebesség számban megadva (1-10)"), MinValue(1), MaxValue(10)] int speed)
    {
        await RespondAsync(embed: await AudioService.SetSpeedAsync(speed, ((ITextChannel) Context.Channel).Guild,
            (SocketUser) Context.User));
        await Task.Delay(5000);
        await DeleteOriginalResponseAsync();
    }

    [SlashCommand("pitch", "Zene hangmagasságának növelése")]
    public async Task Pitch(
        [Summary("pitch", "Hangmagasság számban megadva (1-10)"), MinValue(1), MaxValue(10)] int pitch)
    {
        await RespondAsync(embed: await AudioService.SetPitchAsync(pitch, ((ITextChannel) Context.Channel).Guild,
            (SocketUser) Context.User));
        await Task.Delay(5000);
        await DeleteOriginalResponseAsync();
    }

    [SlashCommand("clearfilter", "Minden aktív szűrőt deaktivál")]
    public async Task ClearFilter()
    {
        await RespondAsync(embed: await AudioService.ClearFiltersAsync(((ITextChannel) Context.Channel).Guild,
            (SocketUser) Context.User));
        await Task.Delay(5000);
        await DeleteOriginalResponseAsync();
    }

    [SlashCommand("clearqueue", "A sorban lévő zenék törlése")]
    public async Task ClearQueue()
    {
        await RespondAsync(embed: await AudioService.ClearQueue(((ITextChannel) Context.Channel).Guild,
            (SocketUser) Context.User));
        await Task.Delay(5000);
        await DeleteOriginalResponseAsync();
    }
}