using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
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

    public override async Task<UserVoteSkipInfo> VoteAsync(ulong userId, float percentage = 0.5f)
    {
        var result = await base.VoteAsync(userId, percentage);

        if (!result.WasSkipped)
        {
            VoteSkipCount = result.Votes.Count;
            VoteSkipRequired =
                result.TotalUsers - (int) Math.Ceiling(result.TotalUsers * percentage);
            return result;
        }

        VoteSkipCount = 0;
        VoteSkipRequired = result.TotalUsers - (int) Math.Ceiling(result.TotalUsers * percentage);

        return result;
    }

    /*public override async Task OnTrackExceptionAsync(TrackExceptionEventArgs eventArgs)
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
    }*/

    public override Task StopAsync(bool disconnect = false)
    {
        IsAutoPlay = false;
        IsLooping = false;
        return base.StopAsync(disconnect);
    }
}