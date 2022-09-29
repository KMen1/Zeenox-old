using Discordance.Extensions;
using Discordance.Models;
using Discordance.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Discordance.Modules.Music;

public class MusicBase : ModuleBase
{
    public AudioService AudioService { get; set; } = null!;
    public IMemoryCache Cache { get; set; } = null!;

    public DiscordancePlayer GetPlayer() => AudioService.GetPlayer(Context.Guild.Id);

    public MusicConfig GetConfig() => Cache.GetGuildConfig(Context.Guild.Id).Music;
}
