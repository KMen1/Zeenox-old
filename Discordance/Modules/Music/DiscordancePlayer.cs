using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discordance.Extensions;
using Discordance.Services;
using Lavalink4NET.Events;
using Lavalink4NET.Player;

namespace Discordance.Modules.Music;

public class DiscordancePlayer : VoteLavalinkPlayer
{
    public SocketVoiceChannel VoiceChannel { get; }
    private readonly LocalizationService _localization;

    public DiscordancePlayer(
        SocketVoiceChannel voiceChannel,
        string lang,
        bool isAnonymous,
        LocalizationService localization
    )
    {
        VoiceChannel = voiceChannel;
        Lang = lang;
        IsAnonymous = isAnonymous;
        _localization = localization;
        IsAutoPlay = false;
        CurrentFilter = "None";
        History = new List<LavalinkTrack>();
        DisposeTokenSource = new CancellationTokenSource();
        Waiting = false;
        Playlist = new List<string>();
        ActionHistory = new List<string>();
    }

    private string Lang { get; }
    private bool IsAnonymous { get; }
    public bool IsAutoPlay { get; private set; }
    private string? CurrentFilter { get; set; }
    public IUser RequestedBy => (IUser)CurrentTrack!.Context!;
    public IUserMessage Message { get; private set; } = null!;
    public ITextChannel TextChannel { get; private set; } = null!;
    public List<LavalinkTrack> History { get; }
    private CancellationTokenSource DisposeTokenSource { get; set; }
    private bool Waiting { get; set; }
    private List<string> Playlist { get; }
    public bool IsPlaying => State is PlayerState.Playing or PlayerState.Paused;
    private List<string> ActionHistory { get; }
    private int VoteSkipCount { get; set; }
    private int VoteSkipRequired { get; set; }

    public void SetMessage(IUserMessage message)
    {
        Message = message;
        TextChannel = (ITextChannel)message.Channel;
    }

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

        Playlist.Add($"[{track.Title}]({track.Uri})");
        await base.PlayAsync(track).ConfigureAwait(false);
        AppendAction(
            _localization
                .GetMessage(Lang, "track_added")
                .FormatWithTimestamp(user.Mention, track.ToHyperLink())
        );
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public Task SetFilterNameAsync(IUser user, string filter)
    {
        CurrentFilter = filter;
        AppendAction(
            _localization.GetMessage(Lang, "set_filter").FormatWithTimestamp(user.Mention, filter)
        );
        return UpdateMessageAsync();
    }

    public Task ToggleLoopAsync(IUser user)
    {
        IsLooping = !IsLooping;
        IsAutoPlay = false;
        AppendAction(
            _localization
                .GetMessage(Lang, IsLooping ? "player_loop_enabled" : "player_loop_disabled")
                .FormatWithTimestamp(user.Mention)
        );
        return UpdateMessageAsync();
    }

    public Task ToggleAutoPlayAsync(IUser user)
    {
        IsAutoPlay = !IsAutoPlay;
        IsLooping = false;
        AppendAction(
            _localization
                .GetMessage(
                    Lang,
                    IsAutoPlay ? "player_autoplay_enabled" : "player_autoplay_disabled"
                )
                .FormatWithTimestamp(user.Mention)
        );
        return UpdateMessageAsync();
    }

    public async Task VoteSkipAsync(IUser user)
    {
        var result = await VoteAsync(user.Id).ConfigureAwait(false);
        if (!result.WasAdded)
            return;

        VoteSkipCount = result.Votes.Count;
        VoteSkipRequired = (int)Math.Ceiling(VoiceChannel.Users.Count / 2.0);
        if (!result.WasSkipped)
        {
            AppendAction(
                _localization
                    .GetMessage(Lang, "player_voteskip")
                    .FormatWithTimestamp(user.Mention, VoteSkipRequired)
            );
            await UpdateMessageAsync().ConfigureAwait(false);
            return;
        }

        VoteSkipCount = 0;
        VoteSkipRequired = (int)Math.Ceiling(VoiceChannel.Users.Count / 2.0);
        AppendAction(
            _localization
                .GetMessage(Lang, "player_voteskipped")
                .FormatWithTimestamp(CurrentTrack.ToHyperLink())
        );
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public async Task SkipAsync(IUser user)
    {
        await base.SkipAsync().ConfigureAwait(false);
        if (CurrentTrack is null)
            return;
        AppendAction(
            _localization
                .GetMessage(Lang, "player_skip")
                .FormatWithTimestamp(user.Mention, CurrentTrack.ToHyperLink())
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
            _localization.GetMessage(Lang, "player_previous").FormatWithTimestamp(user.Mention)
        );
        return PlayAsync(track);
    }

    public async Task PlayPlaylistAsync(IUser user, LavalinkTrack[] tracks)
    {
        await base.PlayAsync(tracks[0]).ConfigureAwait(false);
        Queue.AddRange(tracks[1..]);
        AppendAction(
            _localization
                .GetMessage(Lang, "player_playlist_added")
                .FormatWithTimestamp(user.Mention, tracks.Length)
        );
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public async Task PauseAsync(IUser user)
    {
        await base.PauseAsync().ConfigureAwait(false);
        AppendAction(
            _localization.GetMessage(Lang, "player_pause").FormatWithTimestamp(user.Mention)
        );
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public async Task ResumeAsync(IUser user)
    {
        await base.ResumeAsync().ConfigureAwait(false);
        AppendAction(
            _localization.GetMessage(Lang, "player_resume").FormatWithTimestamp(user.Mention)
        );
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public async Task SetVolumeAsync(IUser user, float volume = 1)
    {
        await base.SetVolumeAsync(volume).ConfigureAwait(false);
        AppendAction(
            _localization
                .GetMessage(Lang, "player_volume")
                .FormatWithTimestamp(user.Mention, (int)(volume * 100))
        );
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public async Task<int> ClearQueue(IUser user)
    {
        var count = Queue.Count;
        Queue.Clear();
        AppendAction(
            _localization.GetMessage(Lang, "player_queue_cleared").FormatWithTimestamp(user.Mention)
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
            .WithDescription(_localization.GetMessage(Lang, "error_playback"))
            .WithColor(Color.Red)
            .Build();
        await TextChannel.SendMessageAsync(embed: eb).ConfigureAwait(false);

        await base.OnTrackExceptionAsync(eventArgs).ConfigureAwait(false);
    }

    private async Task UpdateMessageAsync()
    {
        if (await TextChannel.GetMessageAsync(Message.Id).ConfigureAwait(false) is null)
            Message = await TextChannel
                .SendMessageAsync(embed: GetNowPlayingEmbed(), components: GetMessageComponents())
                .ConfigureAwait(false);
        await Message
            .ModifyAsync(
                x =>
                {
                    x.Embed = GetNowPlayingEmbed();
                    x.Components = GetMessageComponents();
                }
            )
            .ConfigureAwait(false);
    }

    public Embed GetNowPlayingEmbed(LavalinkTrack? track = null)
    {
        var sourceTrack = track ?? CurrentTrack;
        var requester = (IUser)sourceTrack.Context;
        return new EmbedBuilder()
            .WithAuthor(
                _localization.GetMessage(Lang, "now_playing"),
                "https://bestanimations.com/media/discs/895872755cd-animated-gif-9.gif"
            )
            .WithTitle(sourceTrack.Title)
            .WithDescription(string.Join("\n", ActionHistory))
            .WithUrl(sourceTrack.Uri?.ToString() ?? "")
            .WithImageUrl(
                $"https://img.youtube.com/vi/{sourceTrack.TrackIdentifier}/maxresdefault.jpg"
            )
            .WithColor(new Color(31, 31, 31))
            .AddField(
                _localization.GetMessage(Lang, "added_by"),
                IsAnonymous ? requester!.Mention : "`Anonymous`",
                true
            )
            .AddField(_localization.GetMessage(Lang, "channel"), VoiceChannel.Mention, true)
            .AddField(
                _localization.GetMessage(Lang, "length"),
                $"`{sourceTrack.Duration.ToTimeString()}`",
                true
            )
            .AddField(
                _localization.GetMessage(Lang, "volume"),
                $"`{Math.Round(Volume * 100).ToString(CultureInfo.InvariantCulture)}%`",
                true
            )
            .AddField(_localization.GetMessage(Lang, "filter"), $"`{CurrentFilter}`", true)
            .AddField(
                _localization.GetMessage(Lang, "in_queue"),
                $"`{Queue.Count.ToString(CultureInfo.InvariantCulture)}`",
                true
            )
            .Build();
    }

    public MessageComponent GetMessageComponents()
    {
        return new ComponentBuilder()
            .WithButton(
                _localization.GetMessage(Lang, "back"),
                "previous",
                emote: new Emoji("⏮"),
                disabled: History.Count == 0,
                row: 0
            )
            .WithButton(
                State == PlayerState.Paused
                  ? _localization.GetMessage(Lang, "resume")
                  : _localization.GetMessage(Lang, "pause"),
                "pause",
                emote: State == PlayerState.Paused ? new Emoji("▶") : new Emoji("⏸"),
                row: 0
            )
            .WithButton(
                _localization.GetMessage(Lang, "stop"),
                "stop",
                emote: new Emoji("⏹"),
                row: 0
            )
            .WithButton(
                $"Skip [{VoteSkipCount}/{VoteSkipRequired}]",
                "next",
                emote: new Emoji("⏭"),
                disabled: Queue.Count == 0,
                row: 0
            )
            .WithButton(
                _localization.GetMessage(Lang, "volume_down"),
                "volumedown",
                emote: new Emoji("🔉"),
                row: 1,
                disabled: Volume == 0
            )
            .WithButton(
                IsAutoPlay
                  ? _localization.GetMessage(Lang, "autoplay_on")
                  : _localization.GetMessage(Lang, "autoplay_off"),
                "autoplay",
                emote: new Emoji("🔎"),
                row: 1
            )
            .WithButton(
                IsLooping
                  ? _localization.GetMessage(Lang, "loop_on")
                  : _localization.GetMessage(Lang, "loop_off"),
                "repeat",
                emote: new Emoji("🔁"),
                row: 1
            )
            .WithButton(
                _localization.GetMessage(Lang, "volume_up"),
                "volumeup",
                emote: new Emoji("🔊"),
                row: 1,
                disabled: Volume == 1.0f
            )
            .WithSelectMenu(
                new SelectMenuBuilder()
                    .WithPlaceholder(_localization.GetMessage(Lang, "filter_select"))
                    .WithCustomId("filterselectmenu")
                    .WithMinValues(1)
                    .WithMaxValues(1)
                    .AddOption(
                        _localization.GetMessage(Lang, "filter_none"),
                        "None",
                        emote: new Emoji("🗑️")
                    )
                    .AddOption(
                        _localization.GetMessage(Lang, "filter_bassboost"),
                        "Bassboost",
                        emote: new Emoji("🤔")
                    )
                    .AddOption(
                        _localization.GetMessage(Lang, "filter_pop"),
                        "Pop",
                        emote: new Emoji("🎸")
                    )
                    .AddOption(
                        _localization.GetMessage(Lang, "filter_soft"),
                        "Soft",
                        emote: new Emoji("✨")
                    )
                    .AddOption(
                        _localization.GetMessage(Lang, "filter_treblebass"),
                        "Treblebass",
                        emote: new Emoji("🔊")
                    )
                    .AddOption(
                        _localization.GetMessage(Lang, "filter_nightcore"),
                        "Nightcore",
                        emote: new Emoji("🌃")
                    )
                    .AddOption(
                        _localization.GetMessage(Lang, "filter_8d"),
                        "Eightd",
                        emote: new Emoji("🎧")
                    )
                    .AddOption(
                        _localization.GetMessage(Lang, "filter_chinese"),
                        "China",
                        emote: new Emoji("🍊")
                    )
                    .AddOption(
                        _localization.GetMessage(Lang, "filter_vaporwave"),
                        "Vaporwave",
                        emote: new Emoji("💦")
                    )
                    .AddOption(
                        _localization.GetMessage(Lang, "filter_speed_up"),
                        "Doubletime",
                        emote: new Emoji("⏫")
                    )
                    .AddOption(
                        _localization.GetMessage(Lang, "filter_speed_down"),
                        "Slowmotion",
                        emote: new Emoji("⏬")
                    )
                    .AddOption(
                        _localization.GetMessage(Lang, "filter_chipmunk"),
                        "Chipmunk",
                        emote: new Emoji("🐿")
                    )
                    .AddOption(
                        _localization.GetMessage(Lang, "filter_darthvader"),
                        "Darthvader",
                        emote: new Emoji("🤖")
                    )
                    .AddOption(
                        _localization.GetMessage(Lang, "filter_dance"),
                        "Dance",
                        emote: new Emoji("🕺")
                    )
                    .AddOption(
                        _localization.GetMessage(Lang, "filter_vibrato"),
                        "Vibrato",
                        emote: new Emoji("🕸")
                    )
                    .AddOption(
                        _localization.GetMessage(Lang, "filter_tremolo"),
                        "Tremolo",
                        emote: new Emoji("📳")
                    ),
                2
            )
            .Build();
    }

    public async Task WaitForInputAsync()
    {
        await Message
            .ModifyAsync(
                x =>
                {
                    x.Embed = new EmbedBuilder()
                        .WithDescription(_localization.GetMessage(Lang, "player_waiting"))
                        .WithColor(Color.Orange)
                        .Build();
                    x.Components = new ComponentBuilder().Build();
                }
            )
            .ConfigureAwait(false);
        Waiting = true;
        await Task.Delay(TimeSpan.FromMinutes(3), DisposeTokenSource.Token).ConfigureAwait(false);
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
        if (await TextChannel.GetMessageAsync(Message.Id).ConfigureAwait(false) is not null)
        {
            await Message
                .ModifyAsync(
                    x =>
                    {
                        x.Embed = new EmbedBuilder()
                            .WithTitle(_localization.GetMessage(Lang, "party_over"))
                            .WithDescription(
                                $"{_localization.GetMessage(Lang, "songs_played_list")}\n{string.Join("\n", Playlist)}"
                            )
                            .WithColor(Color.Blue)
                            .WithTimestamp(DateTimeOffset.Now)
                            .Build();
                        x.Components = new ComponentBuilder().Build();
                    }
                )
                .ConfigureAwait(false);
        }
        await base.DisposeAsyncCore().ConfigureAwait(false);
    }
}
