using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Models;

namespace KBot.Modules.Gambling.BlackJack;

public class BlackJackCommands : KBotModuleBase
{
    [SlashCommand("blackjack", "Hagyományos Blackjack, másnéven 21")]
    public async Task StartBlackJackAsync([Summary("stake", "Add meg hány szintet szeretnél feltenni")] int stake)
    {
        await DeferAsync().ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        dbUser.GamblingProfile ??= new GamblingProfile();
        dbUser.GamblingProfile.BlackJack ??= new BlackJackProfile();
        if (dbUser.GamblingProfile.Money < stake)
        {
            await FollowupAsync("Nincs elég 🪙KCoin-od ekkora tét rakásához.").ConfigureAwait(false);
            return;
        }
        dbUser.GamblingProfile.Money -= stake;
        await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
        var game = GamblingService.CreateBlackJackGame(Context.User, stake);
        var eb = new EmbedBuilder()
            .WithTitle("Blackjack")
            .WithDescription($"Tét: `{game.Stake}`")
            .WithColor(Color.Gold)
            .WithImageUrl(game.GetTablePicUrl())
            .WithDescription($"Tét: {game.Stake.ToString()} kredit")
            .AddField("Játékos", $"Érték: `{game.GetPlayerSum().ToString()}`", true)
            .AddField("Osztó", "Érték: `?`", true)
            .Build();
        var comp = new ComponentBuilder()
            .WithButton("Hit", $"blackjack-hit:{game.Id}")
            .WithButton("Stand", $"blackjack-stand:{game.Id}")
            .Build();
        await FollowupAsync(embed: eb, components: comp).ConfigureAwait(false);
    }
}
