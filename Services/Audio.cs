using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using KBot.Helpers;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Filters;
using Victoria.Responses.Search;

// ReSharper disable InconsistentNaming

namespace KBot.Services;

public class Audio
{
    private readonly DiscordSocketClient _client;
    private readonly LavaNode _lavaNode;
    private bool bassboost;
    private bool eightD;
    private bool karaoke;
    private bool loop;
    private bool nightcore;
    private bool vaporwave;

    public Audio(DiscordSocketClient client, LavaNode lavaNode)
    {
        _lavaNode = lavaNode;
        _client = client;
    }

    public Task InitializeAsync()
    {
        _client.Ready += OnReadyAsync;
        _lavaNode.OnTrackEnded += OnTrackEnded;
        _lavaNode.OnTrackException += OnTrackException;
        return Task.CompletedTask;
    }

    private static async Task OnTrackException(TrackExceptionEventArgs arg)
    {
        await arg.Player.StopAsync();
        await arg.Player.PlayAsync(arg.Track);
    }

    public async Task<Embed> JoinAsync(IGuild guild, IVoiceChannel vChannel, ITextChannel tChannel, SocketUser user)
    {
        if (!_lavaNode.HasPlayer(guild) || vChannel is null) return await EmbedHelper.MakeJoin(user, vChannel, true);
        await _lavaNode.JoinAsync(vChannel, tChannel);
        return await EmbedHelper.MakeJoin(user, vChannel, false);
    }

    public async Task<Embed> LeaveAsync(IGuild guild, IVoiceChannel vChannel, SocketUser user)
    {
        if (!_lavaNode.HasPlayer(guild) || vChannel is null) return await EmbedHelper.MakeLeave(user, vChannel, true);
        await _lavaNode.LeaveAsync(vChannel);
        return await EmbedHelper.MakeLeave(user, vChannel, false);
    }

    public async Task<Embed> MoveAsync(IGuild guild, IVoiceChannel vChannel, SocketUser user)
    {
        if (!_lavaNode.HasPlayer(guild))
            return await EmbedHelper.MakeMove(user, _lavaNode.GetPlayer(guild), vChannel, true);
        await _lavaNode.MoveChannelAsync(vChannel);
        return await EmbedHelper.MakeMove(user, _lavaNode.GetPlayer(guild), vChannel, false);
    }

    public async Task<Embed> PlayAsync([Remainder] string query, IGuild guild, IVoiceChannel vChannel,
        ITextChannel tChannel, SocketUser user)
    {
        var search = Uri.IsWellFormedUriString(query, UriKind.Absolute)
            ? await _lavaNode.SearchAsync(SearchType.Direct, query)
            : await _lavaNode.SearchYouTubeAsync(query);
        var track = search.Tracks.FirstOrDefault();
        var player = _lavaNode.HasPlayer(guild)
            ? _lavaNode.GetPlayer(guild)
            : await _lavaNode.JoinAsync(vChannel, tChannel);

        if (search.Status == SearchStatus.NoMatches)
            return await EmbedHelper.MakePlay(user, track, player, null, true, false);

        if (player.Track != null && player.PlayerState is PlayerState.Playing ||
            player.PlayerState is PlayerState.Paused)
        {
            player.Queue.Enqueue(track);
            return await EmbedHelper.MakePlay(user, track, player, await track.FetchArtworkAsync(), false, true);
        }

        await player.PlayAsync(track);
        return await EmbedHelper.MakePlay(user, track, player, await track.FetchArtworkAsync(), false, false);
    }

    public async Task<Embed> StopAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player == null) return await EmbedHelper.MakeStop(user, null, true);
        await player.StopAsync();
        return await EmbedHelper.MakeStop(user, player, false);
    }

    public async Task<Embed> SkipAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player == null || player.Queue.Count == 0) return await EmbedHelper.MakeSkip(user, player, null, true);
        await player.SkipAsync();
        return await EmbedHelper.MakeSkip(user, player, await player.Track.FetchArtworkAsync(), false);
    }

    public async Task<Embed> PauseOrResumeAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);

        if (player == null) return await EmbedHelper.MakePauseOrResume(user, null, true, false);

        if (player.PlayerState == PlayerState.Playing)
        {
            await player.PauseAsync();
            return await EmbedHelper.MakePauseOrResume(user, player, false, false);
        }

        await player.ResumeAsync();
        return await EmbedHelper.MakePauseOrResume(user, player, false, true);
    }

    public async Task<Embed> SetVolumeAsync(ushort volume, IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player == null) return await EmbedHelper.MakeVolume(user, null, volume, true);
        await player.UpdateVolumeAsync(volume);
        return await EmbedHelper.MakeVolume(user, player, volume, false);
    }

    public async Task<Embed> SetBassBoostAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player == null) return await EmbedHelper.MakeFilter(user, null, "BASS BOOST", true);

        await player.EqualizerAsync(bassboost ? Filter.BassBoost(true) : Filter.BassBoost(false));
        bassboost = !bassboost;
        return await EmbedHelper.MakeFilter(user, player, "BASS BOOST", false, bassboost);
    }

    public async Task<Embed> SetNightCoreAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player == null) return await EmbedHelper.MakeFilter(user, null, "NIGHTCORE", true);
        await player.ApplyFilterAsync(nightcore ? Filter.NightCore(true) : Filter.NightCore(false));
        nightcore = !nightcore;
        return await EmbedHelper.MakeFilter(user, player, "NIGHTCORE", false, nightcore);
    }

    public async Task<Embed> SetEightDAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player == null) return await EmbedHelper.MakeFilter(user, null, "8D", true);
        await player.ApplyFilterAsync(eightD ? Filter.EightD(true) : Filter.EightD(false));
        eightD = !eightD;
        return await EmbedHelper.MakeFilter(user, player, "8D", false, eightD);
    }

    public async Task<Embed> SetVaporWaveAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player == null) return await EmbedHelper.MakeFilter(user, null, "VAPORWAVE", true);
        await player.ApplyFilterAsync(vaporwave ? Filter.VaporWave(true) : Filter.VaporWave(false));
        vaporwave = !vaporwave;
        return await EmbedHelper.MakeFilter(user, player, "VAPORWAVE", false, vaporwave);
    }

    public async Task<Embed> SetKaraokeAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player == null) return await EmbedHelper.MakeFilter(user, null, "KARAOKE", true);
        await player.ApplyFilterAsync(karaoke ? Filter.Karaoke(true) : Filter.Karaoke(false));
        karaoke = !karaoke;
        return await EmbedHelper.MakeFilter(user, player, "KARAOKE", false, karaoke);
    }

    public async Task<Embed> SetLoopAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is not {PlayerState: PlayerState.Playing}) return await EmbedHelper.MakeLoop(user, null, true);

        loop = !loop;
        return await EmbedHelper.MakeLoop(user, player, loop);
    }

    public async Task<Embed> ClearFiltersAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is not {PlayerState: PlayerState.Playing})
            return await EmbedHelper.MakeFilter(user, null, "CLEAR", true);
        IFilter[] filters =
        {
            Filter.NightCore(false),
            Filter.EightD(false),
            Filter.VaporWave(false),
            Filter.Karaoke(false)
        };
        await player.ApplyFiltersAsync(filters, 100, Filter.BassBoost(false));
        return await EmbedHelper.MakeFilter(user, player, "MINDEN", false);
    }

    public async Task<Embed> SetSpeedAsync(float value, IGuild guild, SocketUser commandUser)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is not {PlayerState: PlayerState.Playing})
            return await EmbedHelper.MakeFilter(commandUser, null, "SEBESSÉG", true);
        await player.ApplyFilterAsync(Filter.Speed(true, value));
        return await EmbedHelper.MakeFilter(commandUser, player, $"SEBESSÉG -> {value}", false, true);
    }

    public async Task<Embed> SetPitchAsync(float value, IGuild guild, SocketUser commandUser)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is not {PlayerState: PlayerState.Playing})
            return await EmbedHelper.MakeFilter(commandUser, null, "HANGMAGASSÁG", true);
        await player.ApplyFilterAsync(Filter.Pitch(true, value));
        return await EmbedHelper.MakeFilter(commandUser, player, $"HANGMAGASSÁG -> {value}", false);
    }

    public async Task<Embed> FastForward(TimeSpan time, IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player == null) return await EmbedHelper.MakeFastForward(user, null, time, true);
        await player.SeekAsync(time);
        return await EmbedHelper.MakeFastForward(user, player, time, false);
    }

    private async Task OnReadyAsync()
    {
        await _lavaNode.ConnectAsync();
    }

    private static bool ShouldPlayNext(TrackEndReason trackEndReason)
    {
        return trackEndReason is TrackEndReason.Finished or TrackEndReason.LoadFailed;
    }

    private async Task OnTrackEnded(TrackEndedEventArgs args)
    {
        if (!ShouldPlayNext(args.Reason)) return;

        var player = args.Player;
        if (!player.Queue.TryDequeue(out var queueable))
        {
            if (loop)
                await player.PlayAsync(args.Track);
            //await player.TextChannel.SendMessageAsync("Queue completed! Please add more tracks to rock n' roll!");
            return;
        }

        if (queueable is not { } track)
            //await player.TextChannel.SendMessageAsync("Next item in queue is not a track.");
            return;
        await args.Player.PlayAsync(track);
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = "KÖVETKEZŐ LEJÁTSZÁSA",
                IconUrl = _client.CurrentUser.GetAvatarUrl()
            },
            Color = Color.Green,
            Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
            Title = track.Title,
            Url = track.Url,
            ImageUrl = await track.FetchArtworkAsync(),
            Footer = new EmbedFooterBuilder
            {
                Text = $"Hossz -> {player.Track.Duration:hh\\:mm\\:ss}"
            }
        };
        await player.TextChannel.SendMessageAsync(string.Empty, false, eb.Build());
    }
}