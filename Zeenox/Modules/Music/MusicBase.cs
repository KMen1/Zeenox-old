using System.Threading.Tasks;
using Discord;
using Lavalink4NET.Player;
using Zeenox.Enums;
using Zeenox.Extensions;
using Zeenox.Models;
using Zeenox.Services;

namespace Zeenox.Modules.Music;

public class MusicBase : ModuleBase
{
    public AudioService AudioService { get; set; } = null!;
    public SearchService SearchService { get; set; } = null!;

    protected MusicPlayer GetPlayer()
    {
        return AudioService.GetPlayer(Context.Guild.Id)!;
    }

    protected MusicConfig GetConfig()
    {
        return Cache.GetGuildConfig(Context.Guild.Id).Music;
    }

    protected Task<LavalinkTrack[]> SearchAsync(string query, AudioService.SearchMode searchMode, int limit = 1)
    {
        return SearchService.SearchAsync(query, Context.User, searchMode, limit);
    }

    protected Task<MusicPlayer> CreatePlayerAsync()
    {
        return AudioService.CreatePlayerAsync(
            Context.Guild.Id,
            ((IVoiceState) Context.User).VoiceChannel,
            (ITextChannel) Context.Channel
        );
    }

    protected Task PlayAsync(LavalinkTrack track)
    {
        return AudioService.PlayAsync(Context.Guild.Id, Context.User, track);
    }

    protected Task PlayAsync(LavalinkTrack[] tracks)
    {
        return AudioService.PlayAsync(Context.Guild.Id, Context.User, tracks);
    }

    protected Task SkipAsync()
    {
        return AudioService.SkipAsync(Context.Guild.Id, Context.User);
    }

    protected Task RewindAsync()
    {
        return AudioService.RewindAsync(Context.Guild.Id, Context.User);
    }

    protected Task<bool> PauseOrResumeAsync()
    {
        return AudioService.PauseOrResumeAsync(Context.Guild.Id, Context.User);
    }

    protected Task<PlayerLoopMode> ToggleLoopAsync()
    {
        return AudioService.ToggleLoopAsync(Context.Guild.Id, Context.User);
    }

    protected Task<bool> ToggleAutoPlayAsync()
    {
        return AudioService.ToggleAutoPlayAsync(Context.Guild.Id, Context.User);
    }

    protected Task<int> SetVolumeAsync(float volume)
    {
        return AudioService.SetVolumeAsync(Context.Guild.Id, Context.User, volume);
    }

    protected Task SetFilterAsync(FilterType filter)
    {
        return AudioService.SetFilterAsync(Context.Guild.Id, Context.User, filter);
    }

    protected Task<int> ClearQueueAsync()
    {
        return AudioService.ClearQueueAsync(Context.Guild.Id, Context.User);
    }
}