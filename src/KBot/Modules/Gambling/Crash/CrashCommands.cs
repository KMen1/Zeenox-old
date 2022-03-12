using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Models;

namespace KBot.Modules.Gambling.Crash;

public class CrashCommands : KBotModuleBase
{
    [SlashCommand("crash", "Szokásos crash játék.")]
    public async Task StartCrash([MinValue(100), MaxValue(1000000)]int bet)
    {
        await DeferAsync().ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        dbUser.GamblingProfile ??= new GamblingProfile();
        dbUser.GamblingProfile.Crash ??= new CrashProfile();
        if (dbUser.GamblingProfile.Money < bet)
        {
            await FollowupAsync("Nincs elég 🪙KCoin-od ekkora tét rakásához.").ConfigureAwait(false);
            return;
        }
        dbUser.GamblingProfile.Money -= bet;
        await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
        var ticks = new DateTime(2016, 1, 1).Ticks;
        var ans = DateTime.Now.Ticks - ticks;
        var id =  ans.ToString("x");
        var msg = await FollowupAsync(embed: new EmbedBuilder()
            .WithTitle("Crash")
            .WithColor(Color.Gold)
            .AddField("Szorzó", "`1.0x`", true)
            .AddField("Profit", "`0`", true)
            .Build(), components: new ComponentBuilder()
                .WithButton(" ", $"crash:{id}:{Context.User.Id}", ButtonStyle.Danger, new Emoji("🛑"))
                .Build())
            .ConfigureAwait(false);
        await GamblingService.StartCrashGameAsync(Context.User, msg, bet).ConfigureAwait(false);
    }

    [ComponentInteraction("crash:*:*")]
    public async Task StopCrash(string id, string userId)
    {
        await DeferAsync().ConfigureAwait(false);
        if (Context.User.Id != Convert.ToUInt64(userId))
            return;
        await GamblingService.StopCrashGameAsync(id).ConfigureAwait(false);
    }
}