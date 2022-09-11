using System.Threading.Tasks;
using Discordance.Models;
using Discordance.Services;

namespace Discordance.Modules.Music;

public class MusicBase : ModuleBase
{
    public AudioService AudioService { get; set; }

    public DiscordancePlayer GetPlayer() => AudioService.GetPlayer(Context.Guild.Id);

    public async Task<MusicConfig> GetConfig()
    {
        return (await DatabaseService.GetGuildConfigAsync(Context.Guild.Id).ConfigureAwait(false)).Music;
    }
}
