using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Humanizer;
using Lavalink4NET.Player;

namespace KBot.Modules.Music;

[Group("music", "Audio parancsok")]
public class MusicCommands : KBotModuleBase
{
    public AudioService AudioService { get; set; }
    
    [SlashCommand("move", "Átlép abba a hangcsatornába, amelyben tartózkodsz")]
    public async Task MovePlayerAsync()
    {
        await RespondAsync(embed: await AudioService.MoveAsync(Context.Guild, Context.User).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    [SlashCommand("leave", "Elhagyja azt a hangcsatornát, amelyben a bot éppen tartózkodik")]
    public async Task DisconnectPlayerAsync()
    {
        await RespondAsync(embed: await AudioService.DisconnectAsync(Context.Guild, Context.User).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    [SlashCommand("play", "Lejátssza a kívánt zenét")]
    public async Task PlayAsync([Summary("query", "Zene linkje vagy címe (YouTube, SoundCloud, Twitch)")] string query)
    {
        await DeferAsync().ConfigureAwait(false);
        if (((IVoiceState)Context.User).VoiceChannel is null)
        {
            await FollowupAsync(embed: new EmbedBuilder().ErrorEmbed("Nem vagy hangcsatornában!")).ConfigureAwait(false);
            return;
        }
        await AudioService.PlayAsync(Context.Guild, Context.Interaction, query).ConfigureAwait(false);
    }

    [SlashCommand("search", "Keres egy zenét a YouTube-on")]
    public async Task SearchAsync([Summary("query", "Zene címe")] string query)
    {
        await DeferAsync().ConfigureAwait(false);
        if (Uri.IsWellFormedUriString(query, UriKind.Absolute))
        {
            await AudioService.PlayAsync(Context.Guild, Context.Interaction, query).ConfigureAwait(false);
            return;
        }
        var search = await AudioService.SearchAsync(query).ConfigureAwait(false);
        if (search is null)
        {
            await FollowupAsync("Nincs találat! Kérlek próbáld újra másképp!").ConfigureAwait(false);
            return;
        }
        var tracks = search.Tracks.ToList();//
        var desc = tracks.Take(10).Aggregate("", (current, track) => current + $"{tracks.TakeWhile(n => n != track).Count() + 1}. [`{track.Title}`]({track.Source}) | [`{track.Duration}`]\n");

        var comp = new ComponentBuilder();
        for (var i = 0; i < tracks.Take(10).Count(); i++)
        {
            var emoji = i switch
            {
                0 => "1️⃣",
                1 => "2️⃣",
                2 => "3️⃣",
                3 => "4️⃣",
                4 => "5️⃣",
                5 => "6️⃣",
                6 => "7️⃣",
                7 => "8️⃣",
                8 => "9️⃣",
                9 => "🔟",
                _ => ""
            };
            comp.WithButton(" ", $"search:{tracks[i].TrackIdentifier}", emote: new Emoji(emoji));
        }
        var eb = new EmbedBuilder()
            .WithTitle("Válaszd ki a kívánt zeneszámot")
            .WithColor(Color.Blue)
            .WithDescription(desc)
            .Build();
        await FollowupAsync(embed: eb, components: comp.Build()).ConfigureAwait(false);
    }
    
    [ComponentInteraction("search:*", true)]
    public async Task PlaySearchAsync(string identifier)
    {
        await DeferAsync().ConfigureAwait(false);
        await AudioService.PlayFromSearchAsync(Context.Guild, (SocketMessageComponent)Context.Interaction, identifier)
            .ConfigureAwait(false);
    }

    [SlashCommand("volume", "Hangerő beállítása")]
    public async Task ChangeVolumeAsync(
        [Summary("volume", "Hangerő számban megadva (1-100)"), MinValue(1), MaxValue(100)] ushort volume)
    {
        await RespondAsync(embed: await AudioService.SetVolumeAsync(Context.Guild, volume).ConfigureAwait(false),
            ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("queue", "A sorban lévő zenék listája")]
    public Task SendQueueAsync()
    {
        return RespondAsync(embed: AudioService.GetQueue(Context.Guild), ephemeral: true);
    }

    [SlashCommand("clearqueue", "A sorban lévő zenék törlése")]
    public async Task ClearQueueAsync()
    {
        await RespondAsync(embed: await AudioService.ClearQueueAsync(Context.Guild).ConfigureAwait(false)).ConfigureAwait(false);
        await Task.Delay(5000).ConfigureAwait(false);
        await DeleteOriginalResponseAsync().ConfigureAwait(false);
    }
}