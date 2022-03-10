using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Models;

namespace KBot.Modules.Gambling.HighLow;
public class HighLowCommands : KBotModuleBase
{
    [SlashCommand("highlow", "Döntsd el hogy az osztónál lévő kártya nagyobb vagy kisebb a tiédnél.")]
    public async Task StartHighLowAsync([Summary("stake", "Add meg a kívánt tétet"), MinValue(20), MaxValue(100)]int stake)
    {
        await DeferAsync().ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild.Id, Context.User.Id).ConfigureAwait(false);
        dbUser.GamblingProfile ??= new GamblingProfile();
        dbUser.GamblingProfile.HighLow ??= new HighLowProfile();
        if (dbUser.GamblingProfile.Money < stake)
        {
            await FollowupAsync("Nincs elég 🪙KCoin-od ekkora tét rakásához.", ephemeral:true).ConfigureAwait(false);
            return;
        }
        dbUser.GamblingProfile.Money -= stake;
        await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
        var game = GamblingService.CreateHighLowGame(Context.User, stake);
        var eb = new EmbedBuilder()
            .WithTitle("High/Low")
            .WithDescription($"Tét: **{game.Stake} kredit**")
            .AddField("Nagyobb", $"Szorzó: **{game.HighMultiplier.ToString()}**\n" +
                                 $"Nyeremény: **{game.HighStake.ToString()} kredit**", true)
            .AddField("Kisebb", $"Szorzó: **{game.LowMultiplier.ToString()}**\n" +
                                $"Nyeremény: **{game.LowStake.ToString()}** kredit", true)
            .WithColor(Color.Gold)
            .WithImageUrl(game.GetTablePicUrl())
            .Build();
        var comp = new ComponentBuilder()
            .WithButton(" ", $"highlow-high:{game.Id}", ButtonStyle.Success, new Emoji("⬆"))
            .WithButton(" ", $"highlow-low:{game.Id}", ButtonStyle.Danger, new Emoji("⬇"))
            .WithButton(" ", "highlow-cancel", emote: new Emoji("❌"), disabled:true)
            .Build();
        await FollowupAsync(embed: eb, components: comp).ConfigureAwait(false);
    }
}