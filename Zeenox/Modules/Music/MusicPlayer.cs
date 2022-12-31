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
        IsAutoPlay = false;
        CurrentFilter = "None";
        History = new List<LavalinkTrack>();
        VoteSkipRequired = (int) Math.Ceiling(voiceChannel.GetConnectedUserCount() * 0.5f);
        LocalizedPlayer = new LocalizedPlayer(cache, language);
    }

    private LocalizedPlayer LocalizedPlayer { get; }
    private IVoiceChannel VoiceChannel { get; }
    private ITextChannel TextChannel { get; }
    private ulong? MessageId { get; set; }
    public bool IsAutoPlay { get; set; }
    public string CurrentFilter { get; set; }
    private List<LavalinkTrack> History { get; }
    private int VoteSkipCount { get; set; }
    private int VoteSkipRequired { get; set; }

    private Embed Embed
    {
        get
        {
            var context = (TrackContext) CurrentTrack?.Context!;

            var eb = new EmbedBuilder()
                .WithAuthor(
                    LocalizedPlayer.NowPlaying,
                    "https://bestanimations.com/media/discs/895872755cd-animated-gif-9.gif"
                )
                .WithTitle(CurrentTrack.SourceName == "spotify"
                    ? $"{CurrentTrack.Author} - {CurrentTrack.Title}"
                    : CurrentTrack.Title)
                .WithUrl(CurrentTrack.Uri?.ToString() ?? "")
                .WithImageUrl(context.CoverUrl)
                .WithColor(new Color(31, 31, 31))
                .AddField(
                    LocalizedPlayer.AddedBy,
                    context.Requester.Mention,
                    true
                )
                .AddField(
                    LocalizedPlayer.Length,
                    $"`{CurrentTrack.Duration.ToTimeString()}`",
                    true
                )
                .AddField(
                    LocalizedPlayer.Volume,
                    $"`{Math.Round(Volume * 100).ToString(CultureInfo.InvariantCulture)}%`",
                    true
                );

            if (CurrentFilter != "None") eb.AddField(LocalizedPlayer.Filter, $"`{CurrentFilter}`", true);

            if (Queue.Count > 0)
                eb.AddField(
                    LocalizedPlayer.InQueue,
                    $"`{Queue.Count.ToString()}`",
                    true
                );

            return eb.Build();
        }
    }

    private MessageComponent Buttons =>
        new ComponentBuilder()
            .WithButton(LocalizedPlayer.Back,
                "previous",
                emote: new Emoji("⏮"),
                disabled: History.Count == 0,
                row: 0
            )
            .WithButton(State == PlayerState.Paused ? LocalizedPlayer.Resume : LocalizedPlayer.Pause,
                "pause",
                emote: State == PlayerState.Paused ? new Emoji("▶") : new Emoji("⏸"),
                row: 0
            )
            .WithButton(
                LocalizedPlayer.Stop,
                "stop",
                emote: new Emoji("⏹"),
                row: 0
            )
            .WithButton(
                $"{LocalizedPlayer.Skip} [{VoteSkipCount.ToString()}/{VoteSkipRequired.ToString()}]",
                "next",
                emote: new Emoji("⏭"),
                disabled: Queue.Count == 0 && !IsAutoPlay,
                row: 0
            )
            .WithButton(
                LocalizedPlayer.Favorite,
                "favorite",
                emote: new Emoji("❤️"),
                disabled: CurrentTrack is null,
                row: 0)
            .WithButton(
                LocalizedPlayer.VolumeDown,
                "volumedown",
                emote: new Emoji("🔉"),
                disabled: Volume == 0,
                row: 1)
            /*.WithButton("AutoPlay " + (IsAutoPlay ? Localized.On : Localized.Off),
                "autoplay",
                emote: new Emoji("🔎"),
                row: 1
            )*/
            .WithButton(LocalizedPlayer.Filter,
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
                LocalizedPlayer.VolumeUp,
                "volumeup",
                emote: new Emoji("🔊"),
                disabled: Math.Abs(Volume - 1.0f) < 0.01f,
                row: 1)
            .Build();

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
        if (!result.WasAdded)
            return result;

        VoteSkipCount = result.Votes.Count;
        VoteSkipRequired =
            result.TotalUsers - (int) Math.Ceiling(result.TotalUsers * percentage);
        await UpdateMessageAsync().ConfigureAwait(false);
        return result;
    }

    public override async Task SkipAsync(int count = 1)
    {
        var voiceChannelCount = VoiceChannel.GetConnectedUserCount();
        VoteSkipCount = 0;
        VoteSkipRequired = voiceChannelCount - (int) Math.Ceiling(voiceChannelCount * 0.5f);
        await base.SkipAsync(count).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
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
        await UpdateMessageAsync().ConfigureAwait(false);
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
        VoteSkipRequired = (int) Math.Ceiling(VoiceChannel.GetConnectedUserCount() * 0.5f);
        if (LoopMode == PlayerLoopMode.None && CurrentTrack is not null)
            History.Add(CurrentTrack);
        return Task.CompletedTask;
    }
}