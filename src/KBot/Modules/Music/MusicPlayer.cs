using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Google.Apis.YouTube.v3;
using KBot.Extensions;
using Lavalink4NET;
using Lavalink4NET.Events;
using Lavalink4NET.Player;

namespace KBot.Modules.Music;

public class MusicPlayer : LavalinkPlayer
{
    public readonly IVoiceChannel VoiceChannel;

    public MusicPlayer(
        IVoiceChannel voiceChannel,
        IUserMessage nowPlayingMessage,
        int skipVotesNeeded,
        YouTubeService youTubeService,
        LavalinkNode lavalinkNode)
    {
        VoiceChannel = voiceChannel;
        SkipVotesNeeded = skipVotesNeeded;
        YouTubeService = youTubeService;
        LavalinkNode = lavalinkNode;
        Loop = false;
        AutoPlay = false;
        FilterEnabled = null;
        NowPlayingMessage = nowPlayingMessage;
        Queue = new List<LavalinkTrack>();
        QueueHistory = new List<LavalinkTrack>();
        SkipVotes = new List<ulong>();
    }

    public bool Loop { get; private set; }
    public string? FilterEnabled { get; set; }
    public SocketUser LastRequestedBy => (CurrentTrack!.Context as TrackContext)!.AddedBy;
    private IUserMessage? NowPlayingMessage { get; set; }
    public List<LavalinkTrack> Queue { get; }
    public int QueueCount => Queue.Count;
    private List<LavalinkTrack> QueueHistory { get; }
    public int QueueHistoryCount => QueueHistory.Count;
    public bool CanGoBack => QueueHistory.Count > 0;
    public bool CanGoForward => Queue.Count > 0;
    public bool IsPlaying => CurrentTrack != null;
    public List<ulong> SkipVotes { get; set; }
    public int SkipVotesNeeded { get; private set; }
    private YouTubeService YouTubeService { get; }
    private LavalinkNode LavalinkNode { get; }
    public bool AutoPlay { get; private set; }

    private Task UpdateNowPlayingMessageAsync()
    {
        return NowPlayingMessage!.ModifyAsync(x =>
        {
            x.Content = "";
            x.Embed = new EmbedBuilder().NowPlayingEmbed(this);
            x.Components = new ComponentBuilder().NowPlayerComponents(this);
        });
    }

    
    public Task ToggleLoopAsync()
    {
        Loop = !Loop;
        return UpdateNowPlayingMessageAsync();
    }
    
    public Task ToggleAutoPlayAsync()
    {
        AutoPlay = !AutoPlay;
        return UpdateNowPlayingMessageAsync();
    }
    
    public Task EnqueueAsync(LavalinkTrack track)
    {
        Queue.Add(track);
        return UpdateNowPlayingMessageAsync();
    }

    public Task EnqueueAsync(IEnumerable<LavalinkTrack> tracks)
    {
        Queue.AddRange(tracks);
        return UpdateNowPlayingMessageAsync();
    }

    public bool TryDequeue(LavalinkTrack track, out LavalinkTrack? nextTrack)
    {
        if (Queue.Count > 0 && Queue.Contains(track))
        {
            nextTrack = Queue[0];
            Queue.Remove(track);
            return true;
        }

        nextTrack = null;
        return false;
    }

    public async Task<bool> ClearQueueAsync()
    {
        if (Queue.Count == 0) return false;
        Queue.Clear();
        await UpdateNowPlayingMessageAsync().ConfigureAwait(false);
        return true;
    }

    public Task SkipAsync()
    {
        if (CurrentTrack == null || Queue.Count == 0) return Task.CompletedTask;
        QueueHistory.Add(CurrentTrack);
        var nextTrack = Queue[0];
        Queue.RemoveAt(0);
        return PlayAsync(nextTrack);
    }

    public Task VoteSkipAsync(IUser user)
    {
        if (SkipVotes.Contains(user.Id)) return Task.CompletedTask;
        SkipVotes.Add(user.Id);
        if (SkipVotes.Count < SkipVotesNeeded) return Task.CompletedTask;
        SkipVotes.Clear();
        return SkipAsync();
    }

    public Task PlayPreviousAsync()
    {
        if (QueueHistory.Count == 0) return Task.CompletedTask;
        var track = QueueHistory.Last();
        QueueHistory.Remove(track);
        return PlayAsync(track);
    }

    public override async Task PlayAsync(LavalinkTrack track, TimeSpan? startTime = null, TimeSpan? endTime = null, bool noReplace = false)
    {
        await base.PlayAsync(track, startTime, endTime, noReplace).ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync().ConfigureAwait(false);
    }

    public override async Task PauseAsync()
    {
        await base.PauseAsync().ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync().ConfigureAwait(false);
    }
    
    public override async Task ResumeAsync()
    {
        await base.ResumeAsync().ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync().ConfigureAwait(false);
    }

    public override async Task SetVolumeAsync(float volume = 1, bool normalize = false, bool force = false)
    {
        await base.SetVolumeAsync(volume, normalize, force).ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync().ConfigureAwait(false);
    }

    public override async Task DisconnectAsync()
    {
        await NowPlayingMessage!.DeleteAsync().ConfigureAwait(false);
        NowPlayingMessage = null;
        await base.DisconnectAsync().ConfigureAwait(false);
    }

    public override async Task OnTrackEndAsync(TrackEndEventArgs args)
    {
        if (!args.MayStartNext) return;
        var player = args.Player;
        if (Loop)
        {
            await player.PlayAsync(Queue[0]).ConfigureAwait(false);
            return;
        }

        var nextTrack = Queue.FirstOrDefault();
        if (nextTrack is not null)
        {
            QueueHistory.Add(Queue[0]);
            Queue.RemoveAt(0);
            await args.Player.PlayAsync(nextTrack).ConfigureAwait(false);
            var users = await VoiceChannel.GetUsersAsync().FlattenAsync().ConfigureAwait(false);
            SkipVotesNeeded = users.Count(x => !x.IsBot) / 2;
            await UpdateNowPlayingMessageAsync().ConfigureAwait(false);
            return;
        }

        if (AutoPlay)
        {
            var searchListRequest = YouTubeService.Search.List("snippet");
            searchListRequest.RelatedToVideoId = CurrentTrack!.TrackIdentifier;
            searchListRequest.Type = "video";
            searchListRequest.MaxResults = 10;

            var result = await searchListRequest.ExecuteAsync().ConfigureAwait(false);
            var next = result.Items.First(x => x.Snippet is not null).Id.VideoId;

            var track = await LavalinkNode.GetTrackAsync($"https://www.youtube.com/watch?v={next}")
                .ConfigureAwait(false);
            track!.Context = CurrentTrack.Context;

            await player.PlayAsync(track).ConfigureAwait(false);
            await UpdateNowPlayingMessageAsync().ConfigureAwait(false);
            return;
        }

        await NowPlayingMessage!.DeleteAsync().ConfigureAwait(false);
        NowPlayingMessage = null;
        await args.Player.DisconnectAsync().ConfigureAwait(false);
        await base.OnTrackEndAsync(args).ConfigureAwait(false);
    }

    public override async Task OnTrackExceptionAsync(TrackExceptionEventArgs args)
    {
        await args.Player.StopAsync().ConfigureAwait(false);
        Queue.RemoveAt(0);
        await args.Player.PlayAsync(Queue[0]).ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync().ConfigureAwait(false);
        await base.OnTrackExceptionAsync(args).ConfigureAwait(false);
    }

    protected override void Dispose(bool disposing)
    {
        NowPlayingMessage?.DeleteAsync().Wait();
        base.Dispose(disposing);
    }
}

public class TrackContext
{
    public TrackContext(SocketUser user)
    {
        AddedBy = user;
    }

    public SocketUser AddedBy { get; }
}