using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Extensions;

namespace KBot.Modules.DeadByDaylight;

public class DbDCommands : SlashModuleBase
{
    private readonly DbDService _dbDService;

    public DbDCommands(DbDService dbDService)
    {
        _dbDService = dbDService;
    }
    
    [DefaultMemberPermissions(GuildPermission.SendMessages)]
    [SlashCommand("shrine", "Gets the current weekly shrines")]
    public async Task DbdShrineAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var embed = _dbDService.CachedPerks.ToEmbedBuilder();
        embed.WithDescription(
            $"🏁 <t:{((DateTimeOffset)DateTime.Today).GetNextWeekday(DayOfWeek.Thursday).ToUnixTimeSeconds()}:R>"
        );
        await FollowupAsync(embed: embed.Build()).ConfigureAwait(false);
    }
}
