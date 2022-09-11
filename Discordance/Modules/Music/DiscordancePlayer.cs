using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discordance.Extensions;
using Lavalink4NET.Events;
using Lavalink4NET.Player;

namespace Discordance.Modules.Music;

public class DiscordancePlayer : QueuedLavalinkPlayer
{
    public SocketVoiceChannel VoiceChannel { get; }

    public DiscordancePlayer(SocketVoiceChannel voiceChannel)
    {
        VoiceChannel = voiceChannel;
        IsAutoPlay = false;
        CurrentFilter = "None";
        Message = null;
        History = new List<LavalinkTrack>();
        SkipVotes = new List<ulong>();
        SkipVotesNeeded = VoiceChannel.ConnectedUsers.Count(x => !x.IsBot) / 2;
        DisposeTokenSource = new CancellationTokenSource();
        Waiting = false;
        Playlist = new List<string>();
        ShowRequester = true;
        ActionHistory = new List<string>();
    }

    public bool IsAutoPlay { get; private set; }
    private string? CurrentFilter { get; set; }
    public IUser RequestedBy => (IUser)CurrentTrack!.Context!;
    public IUserMessage? Message { get; set; }
    public List<LavalinkTrack> History { get; }
    private bool CanSkip => Queue.Count > 0;
    private bool CanGoBack => History.Count > 0;
    private List<ulong> SkipVotes { get; }
    public int SkipVotesNeeded { get; set; }
    private CancellationTokenSource DisposeTokenSource { get; set; }
    private bool Waiting { get; set; }
    private List<string> Playlist { get; }
    public bool ShowRequester { get; set; }
    public bool IsPlaying => State is PlayerState.Playing or PlayerState.Paused;
    private List<string> ActionHistory { get; }

    private void AppendAction(string action)
    {
        ActionHistory.Insert(0, action);
        if (ActionHistory.Count > 5)
            ActionHistory.RemoveAt(5);
    }

    public async Task PlayAsync(IUser user, LavalinkTrack track)
    {
        if (Waiting)
        {
            DisposeTokenSource.Cancel();
            DisposeTokenSource = new CancellationTokenSource();
        }

        Playlist.Add($"[{track.Title}]({track.Source})");
        var t = await base.PlayAsync(track).ConfigureAwait(false);
        AppendAction(
            $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>: *{user.Mention} added [{track.Title}]({track.Source})*"
        );
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public Task SetFilterNameAsync(IUser user, string filter)
    {
        CurrentFilter = filter;
        AppendAction(
            $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>: *{user.Mention} set filter to {filter}*"
        );
        return UpdateMessageAsync();
    }

    public Task ToggleLoopAsync(IUser user)
    {
        IsLooping = !IsLooping;
        IsAutoPlay = false;
        AppendAction(
            $"*<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>: {user.Mention} toggled loop to {IsLooping}*"
        );
        return UpdateMessageAsync();
    }

    public Task ToggleAutoPlayAsync(IUser user)
    {
        IsAutoPlay = !IsAutoPlay;
        IsLooping = false;
        AppendAction(
            $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>: *{user.Mention} toggled autoplay to {IsAutoPlay}*"
        );
        return UpdateMessageAsync();
    }

    public Task VoteSkipAsync(IUser user)
    {
        if (SkipVotes.Contains(user.Id))
            return Task.CompletedTask;
        SkipVotes.Add(user.Id);
        if (SkipVotes.Count < SkipVotesNeeded)
        {
            AppendAction(
                $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>: *{user.Mention} has voted to skip. {SkipVotesNeeded - SkipVotes.Count} more votes needed.*"
            );
            return UpdateMessageAsync();
        }
        SkipVotes.Clear();
        return SkipAsync();
    }

    public async Task SkipAsync(IUser user)
    {
        await base.SkipAsync(1).ConfigureAwait(false);
        if (CurrentTrack is null)
            return;
        AppendAction(
            $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>: *{user.Mention} skipped to [{CurrentTrack.Title}]({CurrentTrack.Source})*"
        );
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public Task PlayPreviousAsync(IUser user)
    {
        if (History.Count == 0)
            return Task.CompletedTask;
        var track = History[^1];
        History.Remove(track);
        AppendAction(
            $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>: *{user.Mention} played [{track.Title}]({track.Source})*"
        );
        return PlayAsync(track);
    }

    public async Task PlayOrEnqueueAsync(IUser user, LavalinkTrack[] tracks, bool isPlaylist)
    {
        if (isPlaylist)
        {
            await base.PlayAsync(tracks[0]).ConfigureAwait(false);
            Queue.AddRange(tracks[1..]);
            AppendAction(
                $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>: *{user.Mention} added {tracks.Length - 1} tracks to the queue.*"
            );
            await UpdateMessageAsync().ConfigureAwait(false);
            return;
        }
        await base.PlayAsync(tracks[0]).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public async Task PauseAsync(IUser user)
    {
        await base.PauseAsync().ConfigureAwait(false);
        AppendAction(
            $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>: *{user.Mention} paused the player*"
        );
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public async Task ResumeAsync(IUser user)
    {
        await base.ResumeAsync().ConfigureAwait(false);
        AppendAction(
            $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>: *{user.Mention} resumed the player*"
        );
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public async Task SetVolumeAsync(IUser user, float volume = 1)
    {
        await base.SetVolumeAsync(volume).ConfigureAwait(false);
        AppendAction(
            $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>: *{user.Mention} set the volume to {volume * 100}%*"
        );
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public async Task<int> ClearQueue(IUser user)
    {
        var count = Queue.Count;
        Queue.Clear();
        AppendAction(
            $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>: *{user.Mention} cleared the queue.*"
        );
        await UpdateMessageAsync().ConfigureAwait(false);
        return count;
    }

    public override async Task OnTrackExceptionAsync(TrackExceptionEventArgs eventArgs)
    {
        if (Queue.Count > 0)
        {
            await SkipAsync().ConfigureAwait(false);
            return;
        }

        var eb = new EmbedBuilder()
            .WithDescription("**Error occured during playback of current track.**")
            .WithColor(Color.Red)
            .Build();
        await Message.Channel.SendMessageAsync(embed: eb).ConfigureAwait(false);

        await base.OnTrackExceptionAsync(eventArgs).ConfigureAwait(false);
    }

    private Task UpdateMessageAsync()
    {
        if (Message is null)
            return Task.CompletedTask;
        return Message.ModifyAsync(
            x =>
            {
                x.Content = "";
                x.Embed = Embed();
                x.Components = Components();
            }
        );
    }

    public Embed Embed(LavalinkTrack? track = null)
    {
        var sourceTrack = track ?? CurrentTrack;
        var requester = (IUser)sourceTrack.Context;
        return new EmbedBuilder()
            .WithAuthor(
                "NOW PLAYING",
                "https://bestanimations.com/media/discs/895872755cd-animated-gif-9.gif"
            )
            .WithTitle(sourceTrack.Title)
            .WithDescription(string.Join("\n", ActionHistory))
            .WithUrl(sourceTrack.Source)
            .WithImageUrl(
                $"https://img.youtube.com/vi/{sourceTrack.TrackIdentifier}/maxresdefault.jpg"
            )
            .WithColor(new Color(31, 31, 31))
            .AddField("👤 Added by", ShowRequester ? requester!.Mention : "`Anonymous`", true)
            .AddField("🎙️ Channel", VoiceChannel.Mention, true)
            .AddField("🕐 Length", $"`{sourceTrack.Duration.ToTimeString()}`", true)
            .AddField(
                "🔊 Volume",
                $"`{Math.Round(Volume * 100).ToString(CultureInfo.InvariantCulture)}%`",
                true
            )
            .AddField("📝 Filter", $"`{CurrentFilter}`", true)
            .AddField(
                "🎶 In Queue",
                $"`{Queue.Count.ToString(CultureInfo.InvariantCulture)}`",
                true
            )
            .Build();
    }

    public MessageComponent Components()
    {
        return new ComponentBuilder()
            .WithButton("Back", "previous", emote: new Emoji("⏮"), disabled: !CanGoBack, row: 0)
            .WithButton(
                State == PlayerState.Paused ? "Resume" : "Pause",
                "pause",
                emote: State == PlayerState.Paused ? new Emoji("▶") : new Emoji("⏸"),
                row: 0
            )
            .WithButton("Stop", "stop", emote: new Emoji("⏹"), row: 0)
            .WithButton(
                $"Skip [{SkipVotes.Count}/{SkipVotesNeeded}]",
                "next",
                emote: new Emoji("⏭"),
                disabled: !CanSkip,
                row: 0
            )
            .WithButton("Down", "volumedown", emote: new Emoji("🔉"), row: 1, disabled: Volume == 0)
            .WithButton(
                IsAutoPlay ? "Autoplay [On]" : "Autoplay [Off]",
                "autoplay",
                emote: new Emoji("🔎"),
                row: 1
            )
            .WithButton(
                IsLooping ? "Loop [On]" : "Loop [Off]",
                "repeat",
                emote: new Emoji("🔁"),
                row: 1
            )
            .WithButton("Up", "volumeup", emote: new Emoji("🔊"), row: 1, disabled: Volume == 1.0f)
            .WithSelectMenu(
                new SelectMenuBuilder()
                    .WithPlaceholder("Select Filter")
                    .WithCustomId("filterselectmenu")
                    .WithMinValues(1)
                    .WithMaxValues(1)
                    .AddOption("None", "None", emote: new Emoji("🗑️"))
                    .AddOption("Bass Boost", "Bassboost", emote: new Emoji("🤔"))
                    .AddOption("Pop", "Pop", emote: new Emoji("🎸"))
                    .AddOption("Soft", "Soft", emote: new Emoji("✨"))
                    .AddOption("Loud", "Treblebass", emote: new Emoji("🔊"))
                    .AddOption("Nightcore", "Nightcore", emote: new Emoji("🌃"))
                    .AddOption("8D", "Eightd", emote: new Emoji("🎧"))
                    .AddOption("Chinese", "China", emote: new Emoji("🍊"))
                    .AddOption("Vaporwave", "Vaporwave", emote: new Emoji("💦"))
                    .AddOption("Speed Up", "Doubletime", emote: new Emoji("⏫"))
                    .AddOption("Speed Down", "Slowmotion", emote: new Emoji("⏬"))
                    .AddOption("Chipmunk", "Chipmunk", emote: new Emoji("🐿"))
                    .AddOption("Darthvader", "Darthvader", emote: new Emoji("🤖"))
                    .AddOption("Dance", "Dance", emote: new Emoji("🕺"))
                    .AddOption("Vibrato", "Vibrato", emote: new Emoji("🕸"))
                    .AddOption("Tremolo", "Tremolo", emote: new Emoji("📳")),
                2
            )
            .Build();
    }

    public async Task WaitForInputAsync()
    {
        await Message!
            .ModifyAsync(
                x =>
                {
                    x.Embed = new EmbedBuilder()
                        .WithDescription(
                            "**Waiting 1 minute for a new song to be added before disconnecting!**"
                        )
                        .WithColor(Color.Orange)
                        .Build();
                    x.Components = new ComponentBuilder().Build();
                }
            )
            .ConfigureAwait(false);
        Waiting = true;
        await Task.Delay(TimeSpan.FromMinutes(1), DisposeTokenSource.Token).ConfigureAwait(false);
        if (!DisposeTokenSource.IsCancellationRequested)
            await DisposeAsync().ConfigureAwait(false);
    }

    public override async Task OnTrackEndAsync(TrackEndEventArgs eventArgs)
    {
        await base.OnTrackEndAsync(eventArgs).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (Message is not null)
        {
            await Message
                .ModifyAsync(
                    x =>
                    {
                        x.Embed = new EmbedBuilder()
                            .WithTitle("Party Over")
                            .WithDescription(
                                $"Listing all songs that were played: \n{string.Join("\n", Playlist)}"
                            )
                            .WithColor(Color.Blue)
                            .Build();
                        x.Components = new ComponentBuilder().Build();
                    }
                )
                .ConfigureAwait(false);
        }
        await base.DisposeAsyncCore().ConfigureAwait(false);
    }
}
