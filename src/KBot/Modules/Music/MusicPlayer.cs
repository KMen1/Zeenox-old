using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Extensions;
using Lavalink4NET.Events;
using Lavalink4NET.Player;

namespace KBot.Modules.Music;

public class MusicPlayer : LavalinkPlayer
{
    public readonly IVoiceChannel VoiceChannel;
    public bool LoopEnabled { get; set; }
    public string FilterEnabled { get; set; }
    public SocketUser LastRequestedBy => (CurrentTrack!.Context as TrackContext)!.AddedBy;
    public IUserMessage NowPlayingMessage { get; set; }
    public List<LavalinkTrack> Queue { get; }
    public int QueueCount => Queue.Count;
    private List<LavalinkTrack> QueueHistory { get; }
    public int QueueHistoryCount => QueueHistory.Count;
    public bool CanGoBack => QueueHistory.Count > 0;
    public bool CanGoForward => Queue.Count > 0;
    public bool IsPlaying => CurrentTrack != null;
    public List<ulong> SkipVotes { get; set; }
    public int SkipVotesNeeded { get; private set; }

    public MusicPlayer(IVoiceChannel voiceChannel, int skipVotesNeeded)
    {
        VoiceChannel = voiceChannel;
        SkipVotesNeeded = skipVotesNeeded;
        LoopEnabled = false;
        FilterEnabled = null;
        NowPlayingMessage = null;
        Queue = new List<LavalinkTrack>();
        QueueHistory = new List<LavalinkTrack>();
        SkipVotes = new List<ulong>();
    }

    public Task UpdateNowPlayingMessageAsync()
    {
        return NowPlayingMessage.ModifyAsync(x =>
        {
            x.Embed = new EmbedBuilder().NowPlayingEmbed(this);
            x.Components = new ComponentBuilder().NowPlayerComponents(this);
        });
    }
    
    public void Enqueue(LavalinkTrack track)
    {
        Queue.Add(track);
    }
    
    public void Enqueue(IEnumerable<LavalinkTrack> tracks)
    {
        Queue.AddRange(tracks);
    }

    public bool TryDequeue(LavalinkTrack track, out LavalinkTrack nextTrack)
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
    
    public bool ClearQueue()
    {
        if (Queue.Count == 0) return false;
        Queue.Clear();
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

    public override async Task OnTrackEndAsync(TrackEndEventArgs args)
    {
        if (!args.MayStartNext)
        {
            return;
        }
        var player = args.Player;
        if (LoopEnabled)
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
        await NowPlayingMessage.DeleteAsync().ConfigureAwait(false);
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