using System.Threading.Tasks;
using Discord;
using Discordance.Enums;
using Discordance.Extensions;
using Discordance.Models;
using Discordance.Services;
using Lavalink4NET.Player;
using Microsoft.Extensions.Caching.Memory;

namespace Discordance.Modules.Music;

public class MusicBase : ModuleBase
{
    public AudioService AudioService { get; set; } = null!;

    protected DiscordancePlayer GetPlayer()
    {
        return AudioService.GetPlayer(Context.Guild.Id)!;
    }

    protected MusicConfig GetConfig()
    {
        return Cache.GetGuildConfig(Context.Guild.Id).Music;
    }

    protected Task<LavalinkTrack[]?> SearchAsync(string query)
    {
        return AudioService.SearchAsync(query, Context.User);
    }

    protected Task<(DiscordancePlayer, bool)> GetOrCreatePlayerAsync()
    {
        return AudioService.GetOrCreatePlayerAsync(
            Context.Guild.Id,
            ((IVoiceState) Context.User).VoiceChannel,
            (ITextChannel) Context.Channel
        );
    }

    protected Task<(Embed?, MessageComponent?)> PlayAsync(LavalinkTrack track)
    {
        return AudioService.PlayAsync(Context.Guild.Id, Context.User, track);
    }

    protected Task<(Embed?, MessageComponent?)> PlayAsync(LavalinkTrack[] tracks)
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

    protected Task PauseOrResumeAsync()
    {
        return AudioService.PauseOrResumeAsync(Context.Guild.Id, Context.User);
    }

    protected Task ToggleLoopAsync()
    {
        return AudioService.ToggleLoopAsync(Context.Guild.Id, Context.User);
    }

    protected Task ToggleAutoPlayAsync()
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