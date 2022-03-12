using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Modules.Music.Helpers;
using Lavalink4NET.Events;
using Lavalink4NET.Player;

namespace KBot.Modules.Music;

public class MusicPlayer : LavalinkPlayer
{
    public readonly IVoiceChannel VoiceChannel;
    public readonly ITextChannel _textChannel;
    
    public bool LoopEnabled { get; set; }
    public string FilterEnabled { get; set; }
    public SocketUser LastRequestedBy => (CurrentTrack!.Context as TrackContext)!.AddedBy;
    public IUserMessage NowPlayingMessage { get; set; }
    public List<LavalinkTrack> Queue { get; }
    public List<LavalinkTrack> QueueHistory { get; }
    public MusicPlayer(IVoiceChannel voiceChannel, ITextChannel textChannel)
    {
        VoiceChannel = voiceChannel;
        _textChannel = textChannel;
        LoopEnabled = false;
        FilterEnabled = null;
        NowPlayingMessage = null;
        Queue = new List<LavalinkTrack>();
        QueueHistory = new List<LavalinkTrack>();
    }

    public class TrackContext
    {
        public TrackContext(SocketUser user)
        {
            AddedBy = user;
        }

        public SocketUser AddedBy { get; }
    }

    public bool CanGoBack => QueueHistory.Count > 0;
    public bool CanGoForward => Queue.Count > 0;

    public Task UpdateNowPlayingMessageAsync()
    {
        var message = NowPlayingMessage;
        var user = LastRequestedBy ?? message.Interaction.User as SocketUser;
        var embed = Embeds.NowPlayingEmbed(user, this);
        var components = Components.NowPlayingComponents(this);
        return message.ModifyAsync(x =>
         {
             x.Embed = embed;
             x.Components = components;
         });
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
        await _textChannel.SendMessageAsync(embed: Embeds.ErrorEmbed(args.ErrorMessage)).ConfigureAwait(false);
        await args.Player.DisconnectAsync().ConfigureAwait(false);
        await NowPlayingMessage.DeleteAsync().ConfigureAwait(false);
        NowPlayingMessage = null;
        await base.OnTrackExceptionAsync(args).ConfigureAwait(false);
    }
}