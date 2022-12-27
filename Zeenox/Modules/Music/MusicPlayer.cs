using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Lavalink4NET.Events;
using Lavalink4NET.Player;
using Microsoft.Extensions.Caching.Memory;
using Zeenox.Enums;
using Zeenox.Extensions;
using Zeenox.Models;

namespace Zeenox.Modules.Music;

public class MusicPlayer : VoteLavalinkPlayer
{
    public MusicPlayer(IVoiceChannel voiceChannel, ITextChannel textChannel, string language, IMemoryCache cache)
    {
        VoiceChannel = voiceChannel;
        TextChannel = textChannel;
        Language = language;
        IsAutoPlay = false;
        CurrentFilter = "None";
        History = new List<LavalinkTrack>();
        Actions = new List<string>();
        VoteSkipRequired = (int) Math.Ceiling(voiceChannel.GetConnectedUserCount() * 0.5f);
        Localized = SetUpLocalizedStrings(cache);
    }

    private LocalizedStrings Localized { get; }
    public string Language { get; }
    private IVoiceChannel VoiceChannel { get; }
    public bool IsAutoPlay { get; set; }
    public string? CurrentFilter { get; set; }
    public IUser RequestedBy => ((TrackContext) CurrentTrack?.Context!).Requester;
    private ulong? MessageId { get; set; }
    private ITextChannel TextChannel { get; }
    public List<LavalinkTrack> History { get; }
    private List<string> Actions { get; }
    public int VoteSkipCount { get; private set; }
    public int VoteSkipRequired { get; private set; }
    public TimeSpan? SponsorBlockSkipTime { get; set; }
    public TimeSpan? LengthWithSponsorBlock => CurrentTrack?.Duration - SponsorBlockSkipTime;

    private Embed Embed
    {
        get
        {
            var context = (TrackContext) CurrentTrack?.Context!;
            return new EmbedBuilder()
                .WithAuthor(
                    Localized.NowPlaying,
                    "https://bestanimations.com/media/discs/895872755cd-animated-gif-9.gif"
                )
                .WithTitle(CurrentTrack.SourceName == "spotify"
                    ? $"{CurrentTrack.Author} - {CurrentTrack.Title}"
                    : CurrentTrack.Title)
                .WithDescription(string.Join("\n", Actions))
                .WithUrl(CurrentTrack.Uri?.ToString() ?? "")
                .WithImageUrl(context.CoverUrl)
                .WithColor(new Color(31, 31, 31))
                .AddField(
                    Localized.AddedBy,
                    context.Requester.Mention,
                    true
                )
                .AddField(Localized.Channel, VoiceChannel.Mention, true)
                .AddField(
                    Localized.Length,
                    $"`{(LengthWithSponsorBlock is not null ? LengthWithSponsorBlock?.ToTimeString() : CurrentTrack.Duration.ToTimeString())}`",
                    true
                )
                .AddField(
                    Localized.Volume,
                    $"`{Math.Round(Volume * 100).ToString(CultureInfo.InvariantCulture)}%`",
                    true
                )
                .AddField(Localized.Filter, $"`{CurrentFilter}`", true)
                .AddField(
                    Localized.InQueue,
                    $"`{Queue.Count.ToString()}`",
                    true
                )
                .Build();
        }
    }

    private MessageComponent Buttons =>
        new ComponentBuilder()
            .WithButton(Localized.Back,
                "previous",
                emote: new Emoji("⏮"),
                disabled: History.Count == 0,
                row: 0
            )
            .WithButton(State == PlayerState.Paused ? Localized.Resume : Localized.Pause,
                "pause",
                emote: State == PlayerState.Paused ? new Emoji("▶") : new Emoji("⏸"),
                row: 0
            )
            .WithButton(
                Localized.Stop,
                "stop",
                emote: new Emoji("⏹"),
                row: 0
            )
            .WithButton(
                $"{Localized.Skip} [{VoteSkipCount.ToString()}/{VoteSkipRequired.ToString()}]",
                "next",
                emote: new Emoji("⏭"),
                disabled: Queue.Count == 0 && !IsAutoPlay,
                row: 0
            )
            .WithButton(
                Localized.Favorite,
                "favorite",
                emote: new Emoji("⭐"),
                disabled: CurrentTrack is null,
                row: 0)
            .WithButton(
                Localized.VolumeDown,
                "volumedown",
                emote: new Emoji("🔉"),
                disabled: Volume == 0,
                row: 1)
            /*.WithButton("AutoPlay " + (IsAutoPlay ? Localized.On : Localized.Off),
                "autoplay",
                emote: new Emoji("🔎"),
                row: 1
            )*/
            .WithButton(Localized.Filter,
                "filter",
                emote: new Emoji("🎚"),
                row: 1
            )
            .WithButton("Loop " + LoopMode switch
                {
                    PlayerLoopMode.Track => "[Track]",
                    PlayerLoopMode.Queue => "[Queue]",
                    _ => "[Off]"
                },
                "repeat",
                emote: new Emoji("🔁"),
                row: 1
            )
            .WithButton(
                Localized.VolumeUp,
                "volumeup",
                emote: new Emoji("🔊"),
                disabled: Math.Abs(Volume - 1.0f) < 0.01f,
                row: 1)
            .Build();

    private LocalizedStrings SetUpLocalizedStrings(IMemoryCache cache)
    {
        return new LocalizedStrings(
            cache.GetMessage(Language, "NowPlaying"),
            cache.GetMessage(Language, "AddedBy"),
            cache.GetMessage(Language, "Channel"),
            cache.GetMessage(Language, "Length"),
            cache.GetMessage(Language, "Volume"),
            cache.GetMessage(Language, "VolUp"),
            cache.GetMessage(Language, "VolDown"),
            cache.GetMessage(Language, "Favorite"),
            cache.GetMessage(Language, "Filter"),
            cache.GetMessage(Language, "InQueue"),
            cache.GetMessage(Language, "Back"),
            cache.GetMessage(Language, "Skip"),
            cache.GetMessage(Language, "Pause"),
            cache.GetMessage(Language, "Resume"),
            cache.GetMessage(Language, "Stop"),
            cache.GetMessage(Language, "On"),
            cache.GetMessage(Language, "Off")
        );
    }

    public async Task<IUserMessage> GetMessage()
    {
        if (MessageId is null)
        {
            var firstMessage =
                await TextChannel.SendMessageAsync(embed: Embed, components: Buttons).ConfigureAwait(false);
            MessageId = firstMessage.Id;
            return firstMessage;
        }

        var message = await TextChannel.GetMessageAsync(MessageId.Value).ConfigureAwait(false);
        if (message is not null)
            return (IUserMessage) message;

        message = await TextChannel.SendMessageAsync(embed: Embed, components: Buttons).ConfigureAwait(false);
        MessageId = message.Id;
        return (IUserMessage) message;
    }

    public Task RemoveFromQueue(int index)
    {
        Queue.RemoveAt(index);
        return UpdateMessageAsync();
    }

    public void AppendAction(string action)
    {
        Actions.Insert(0, action);
        if (Actions.Count > 5)
            Actions.RemoveAt(5);
    }

    public Task ToggleLoopAsync(PlayerLoopMode loopMode)
    {
        LoopMode = loopMode;
        IsAutoPlay = false;
        return UpdateMessageAsync();
    }

    public Task ToggleAutoPlayAsync()
    {
        IsAutoPlay = !IsAutoPlay;
        LoopMode = PlayerLoopMode.None;
        return UpdateMessageAsync();
    }

    private async Task UpdateMessageAsync()
    {
        var message = await GetMessage().ConfigureAwait(false);

        await message
            .ModifyAsync(
                x =>
                {
                    x.Embed = Embed;
                    x.Components = Buttons;
                }
            )
            .ConfigureAwait(false);
    }

    public override async Task<UserVoteSkipInfo> VoteAsync(ulong userId, float percentage = 0.5f)
    {
        var result = await base.VoteAsync(userId, percentage).ConfigureAwait(false);
        if (result.WasSkipped) return result;
        VoteSkipCount = result.Votes.Count;
        VoteSkipRequired =
            result.TotalUsers - (int) Math.Ceiling(result.TotalUsers * percentage);
        return result;
    }

    public override Task SkipAsync(int count = 1)
    {
        var voiceChannelCount = VoiceChannel.GetConnectedUserCount();
        VoteSkipCount = 0;
        VoteSkipRequired = voiceChannelCount - (int) Math.Ceiling(voiceChannelCount * 0.5f);
        return base.SkipAsync(count);
    }

    public override async Task<int> PlayAsync(LavalinkTrack track, TimeSpan? startTime = null, TimeSpan? endTime = null,
        bool noReplace = false)
    {
        var result = await base.PlayAsync(track, startTime, endTime, noReplace).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
        return result;
    }

    public override async Task<int> PlayAsync(LavalinkTrack track, bool enqueue, TimeSpan? startTime = null,
        TimeSpan? endTime = null,
        bool noReplace = false)
    {
        var result = await base.PlayAsync(track, enqueue, startTime, endTime, noReplace).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
        return result;
    }

    public async Task PlayAsync(LavalinkTrack[] tracks)
    {
        await PlayAsync(tracks[0]).ConfigureAwait(false);
        Queue.AddRange(tracks[1..]);
    }

    public async Task RewindAsync()
    {
        if (History.Count == 0) return;

        var track = History[^1];
        History.Remove(track);
        await PlayAsync(track, false).ConfigureAwait(false);
    }

    public async Task ApplyFiltersAsync(FilterType filterType)
    {
        if (filterType is not FilterType.None)
            Filters.Clear();
        Filters.ApplyFilter(filterType);
        await Filters.CommitAsync().ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public override async Task PauseAsync()
    {
        await base.PauseAsync().ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public override async Task ResumeAsync()
    {
        await base.ResumeAsync().ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public override async Task SetVolumeAsync(float volume = 1, bool normalize = false, bool force = false)
    {
        await base.SetVolumeAsync(volume, normalize, force).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public async Task<int> ClearQueueAsync()
    {
        var count = Queue.Count;
        Queue.Clear();
        await UpdateMessageAsync().ConfigureAwait(false);
        return count;
    }

    public override Task OnTrackEndAsync(TrackEndEventArgs eventArgs)
    {
        VoteSkipCount = 0;
        return Task.CompletedTask;
    }

    private record LocalizedStrings(
        string NowPlaying,
        string AddedBy,
        string Channel,
        string Length,
        string Volume,
        string VolumeUp,
        string VolumeDown,
        string Favorite,
        string Filter,
        string InQueue,
        string Back,
        string Skip,
        string Pause,
        string Resume,
        string Stop,
        string On,
        string Off
    );
}