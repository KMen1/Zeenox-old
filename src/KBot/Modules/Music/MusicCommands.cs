using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace KBot.Modules.Music;

[Group("music", "Music")]
public class MusicCommands : SlashModuleBase
{
    private readonly MusicService _audioService;

    public MusicCommands(MusicService audioService)
    {
        _audioService = audioService;
    }

    [SlashCommand("move", "Moves the bot to the channel you are in")]
    public async Task MovePlayerAsync()
    {
        var channel = ((IVoiceState)Context.User).VoiceChannel;
        if (channel is null)
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**You are not in a voice channel**")
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await RespondAsync(
                embed: await _audioService.MoveAsync(Context.Guild, channel).ConfigureAwait(false),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [SlashCommand("leave", "Leaves the voice channel the bot is in")]
    public async Task DisconnectPlayerAsync()
    {
        await RespondAsync(
                embed: await _audioService
                    .DisconnectAsync(Context.Guild, Context.User)
                    .ConfigureAwait(false),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [SlashCommand("play", "Plays a song")]
    public async Task PlayAsync(string query)
    {
        if (((IVoiceState)Context.User).VoiceChannel is null)
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**You are not in a voice channel**")
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        var isPlaying = _audioService.IsPlayingInGuild(Context.Guild);
        if (isPlaying)
        {
            await DeferAsync(true).ConfigureAwait(false);
            var embed = await _audioService
                .PlayAsync(Context.Guild, (SocketGuildUser)Context.User, null!, query)
                .ConfigureAwait(false);
            await FollowupAsync(embed: embed).ConfigureAwait(false);
        }
        else
        {
            await DeferAsync().ConfigureAwait(false);
            var msg = await FollowupWithEmbedAsync(Color.Orange, "Starting player...", "")
                .ConfigureAwait(false);
            await _audioService
                .PlayAsync(Context.Guild, (SocketGuildUser)Context.User, msg, query)
                .ConfigureAwait(false);
        }
    }

    [SlashCommand("search", "Searches for a song")]
    public async Task SearchAsync(string query)
    {
        if (Uri.IsWellFormedUriString(query, UriKind.Absolute))
        {
            var eEb = new EmbedBuilder()
                .WithDescription(
                    "**Please use /music play <url> if you want to play a song from a url**"
                )
                .WithColor(Color.Red)
                .Build();
            await RespondAsync(embed: eEb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        var search = await _audioService.SearchAsync(query).ConfigureAwait(false);
        if (search is null)
        {
            var eeb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**No matches, please try again!**")
                .Build();
            await FollowupAsync(embed: eeb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        var tracks = search.Tracks!.ToList();
        var desc = tracks
            .Take(10)
            .Aggregate(
                "",
                (current, track) =>
                    current
                    + $"{tracks.TakeWhile(n => n != track).Count() + 1}. [`{track.Title}`]({track.Source}) | [`{track.Duration}`]\n"
            );

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
            .WithTitle("Please select a song")
            .WithColor(Color.Blue)
            .WithDescription(desc)
            .Build();
        await FollowupAsync(embed: eb, components: comp.Build()).ConfigureAwait(false);
    }

    [ComponentInteraction("search:*", true)]
    public async Task PlaySearchAsync(string identifier)
    {
        var channel = ((IVoiceState)Context.User).VoiceChannel;
        if (channel is null)
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**You are not in a voice channel**")
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        var msg = ((SocketMessageComponent)Context.Interaction).Message;
        var result = await _audioService
            .PlayFromSearchAsync(Context.Guild, (SocketGuildUser)Context.User, msg, identifier)
            .ConfigureAwait(false);
        if (result is null)
            return;
        await FollowupAsync(embed: result, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("volume", "Sets the volume")]
    public async Task ChangeVolumeAsync(
        [Summary("volume")] [MinValue(1)] [MaxValue(100)] ushort volume
    )
    {
        await RespondAsync(
                embed: await _audioService
                    .SetVolumeAsync(Context.Guild, volume)
                    .ConfigureAwait(false),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [SlashCommand("queue", "Shows the current queue")]
    public Task SendQueueAsync()
    {
        return RespondAsync(embed: _audioService.GetQueue(Context.Guild), ephemeral: true);
    }

    [SlashCommand("clearqueue", "Clears the current queue")]
    public async Task ClearQueueAsync()
    {
        await RespondAsync(
                embed: await _audioService.ClearQueueAsync(Context.Guild).ConfigureAwait(false),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }
}
