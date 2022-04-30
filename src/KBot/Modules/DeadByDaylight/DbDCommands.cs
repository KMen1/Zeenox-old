using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Extensions;

namespace KBot.Modules.DeadByDaylight;

[Group("dbd", "Commands related to Dead by Daylight")]
public class DbDCommands : SlashModuleBase
{
    private readonly DbDService _dbDService;

    public DbDCommands(DbDService dbDService)
    {
        _dbDService = dbDService;
    }

    [SlashCommand("shrine", "Gets the current weekly shrines")]
    public async Task DbdShrineAsync()
    {
        var sw = Stopwatch.StartNew();
        await DeferAsync().ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithTitle("Shrine of Secrets")
            .WithColor(Color.Orange);

        var perks = await _dbDService.GetShrinesAsync().ConfigureAwait(false);

        foreach (var perk in perks) eb.AddField(perk.Name, $"from {perk.CharacterName}", true);
        eb.WithDescription($"🏁 <t:{((DateTimeOffset)DateTime.Today).GetNextWeekday(DayOfWeek.Thursday).ToUnixTimeSeconds()}:R>");
        sw.Stop();
        eb.WithFooter($"{sw.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture)} ms");
        await FollowupAsync(embed: eb.Build()).ConfigureAwait(false);
    }
    
    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("set", "Sets the channel to receive weekyl shrines")]
    public async Task SetDbdChannelAsync(ITextChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.DbdNotificationChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(channel is null ? Color.Red : Color.Green)
            .WithDescription(channel is null
                ? "**Weekly shrine notifications are now disabled**"
                : $"**Weekly shrine notifications will be sent to {channel.Mention}**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }
}