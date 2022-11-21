using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discordance.Extensions;
using Lavalink4NET.Events;
using Lavalink4NET.Player;

namespace Discordance.Modules.Music;

public class DiscordancePlayer : VoteLavalinkPlayer
{
    public DiscordancePlayer(IVoiceChannel voiceChannel, ITextChannel textChannel)
    {
        VoiceChannel = voiceChannel;
        TextChannel = textChannel;
        IsAutoPlay = false;
        CurrentFilter = "None";
        History = new List<LavalinkTrack>();
        Actions = new List<string>();
        VoteSkipRequired = (int) Math.Ceiling(voiceChannel.GetConnectedUserCount() * 0.5f);
    }

    public IVoiceChannel VoiceChannel { get; }
    public bool IsAutoPlay { get; set; }
    public string? CurrentFilter { get; set; }
    public IUser RequestedBy => (IUser) CurrentTrack!.Context!;
    public ulong? MessageId { get; private set; }
    public ITextChannel TextChannel { get; private set; }
    public List<LavalinkTrack> History { get; }
    public List<string> Actions { get; }
    public int VoteSkipCount { get; private set; }
    public int VoteSkipRequired { get; private set; }
    public TimeSpan? SponsorBlockSkipTime { get; set; }

    public TimeSpan? LengthWithSponsorBlock =>
        SponsorBlockSkipTime is null ? null : CurrentTrack!.Duration - SponsorBlockSkipTime;

    public void SetMessage(IUserMessage message)
    {
        MessageId = message.Id;
        TextChannel = (ITextChannel) message.Channel;
    }

    public void AppendAction(string action)
    {
        Actions.Insert(0, action);
        if (Actions.Count > 5)
            Actions.RemoveAt(5);
    }

    public void ToggleLoop()
    {
        IsLooping = !IsLooping;
        IsAutoPlay = false;
    }

    public void ToggleAutoPlay()
    {
        IsAutoPlay = !IsAutoPlay;
        IsLooping = false;
    }

    public override async Task<UserVoteSkipInfo> VoteAsync(ulong userId, float percentage = 0.5f)
    {
        var result = await base.VoteAsync(userId, percentage);

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

    public override Task OnTrackEndAsync(TrackEndEventArgs eventArgs)
    {
        VoteSkipCount = 0;
        return base.OnTrackEndAsync(eventArgs);
    }
}