using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Extensions;

namespace KBot.Modules.DeadByDaylight;

[Group("dbd", "Commands related to Dead by Daylight")]
public class DbDCommands : SlashModuleBase
{
    public DbDService DbDService { get; set; }

    [SlashCommand("shrine", "Gets the current weekly shrines")]
    public async Task DbdShrineAsync()
    {
        var sw = Stopwatch.StartNew();
        await DeferAsync().ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithTitle("Shrine of Secrets")
            .WithColor(Color.Orange);

        var perks = await DbDService.GetShrinesAsync().ConfigureAwait(false);

        foreach (var perk in perks) eb.AddField(perk.Name, $"from {perk.CharacterName}", true);
        eb.WithDescription($"🏁 <t:{DateTime.Today.GetNextWeekday(DayOfWeek.Thursday).ToUnixTimeSeconds()}:R>");
        sw.Stop();
        eb.WithFooter($"{sw.ElapsedMilliseconds.ToString()} ms");
        await FollowupAsync(embed: eb.Build()).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("set", "Sets the channel to receive weekyl shrines")]
    public async Task SetDbdChannelAsync(ITextChannel channel)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.DbdNotificationChannelId = channel.Id).ConfigureAwait(false);
        await RespondAsync("Channel set to receive weekly shrines", ephemeral: true).ConfigureAwait(false);
    }
}