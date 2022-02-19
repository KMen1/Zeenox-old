using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.DeadByDaylight;

[Group("dbd", "Dead By Daylight parancsok")]
public class DbDCommands : KBotModuleBase
{
    [SlashCommand("shrine", "Heti perkek lekérése")]
    public async Task DbdShrineAsync()
    {
        var sw = Stopwatch.StartNew();
        await DeferAsync().ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithTitle("Shrine of Secrets")
            .WithColor(Color.Orange);

        var (perks, endTime) = await DbDService.GetWeeklyShrinesAsync().ConfigureAwait(false);

        foreach (var perk in perks)
        {
            eb.AddField(perk.Name , $"from {perk.CharacterName}", true);
        }
        eb.WithDescription($"🏁 <t:{endTime}:R>");
        sw.Stop();
        eb.WithFooter($"{sw.ElapsedMilliseconds.ToString()} ms");
        await FollowupAsync(embed: eb.Build()).ConfigureAwait(false);
    }
}