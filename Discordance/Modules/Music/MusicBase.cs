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
    private AudioService AudioService { get; set; } = null!;
    private IMemoryCache Cache { get; set; } = null!;

    protected DiscordancePlayer GetPlayer() => AudioService.GetPlayer(Context.Guild.Id);

    protected MusicConfig GetConfig() => Cache.GetGuildConfig(Context.Guild.Id).Music;

    protected Task<LavalinkTrack[]?> SearchAsync(string query) =>
        AudioService.SearchAsync(query, Context.User);

    protected Task<(DiscordancePlayer, bool)> GetOrCreatePlayerAsync() =>
        AudioService.GetOrCreatePlayerAsync(
            Context.Guild.Id,
            ((IVoiceState)Context.User).VoiceChannel,
            (ITextChannel)Context.Channel
        );

    protected Task<(Embed?, MessageComponent?)> PlayAsync(LavalinkTrack track) =>
        AudioService.PlayAsync(Context.Guild.Id, Context.User, track);

    protected Task<(Embed?, MessageComponent?)> PlayAsync(LavalinkTrack[] tracks) =>
        AudioService.PlayAsync(Context.Guild.Id, Context.User, tracks);

    protected Task SkipAsync() => AudioService.SkipAsync(Context.Guild.Id, Context.User);

    protected Task RewindAsync() => AudioService.RewindAsync(Context.Guild.Id, Context.User);

    protected Task PauseOrResumeAsync() =>
        AudioService.PauseOrResumeAsync(Context.Guild.Id, Context.User);

    protected Task ToggleLoopAsync() =>
        AudioService.ToggleLoopAsync(Context.Guild.Id, Context.User);

    protected Task ToggleAutoPlayAsync() =>
        AudioService.ToggleAutoPlayAsync(Context.Guild.Id, Context.User);

    protected Task<int> SetVolumeAsync(float volume) =>
        AudioService.SetVolumeAsync(Context.Guild.Id, Context.User, volume);

    protected Task SetFilterAsync(FilterType filter) =>
        AudioService.SetFilterAsync(Context.Guild.Id, Context.User, filter);

    protected Task<int> ClearQueueAsync() =>
        AudioService.ClearQueueAsync(Context.Guild.Id, Context.User);
}
