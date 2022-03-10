using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace KBot.Modules.Gambling.BlackJack;

public class BlackJackComponents : KBotModuleBase
{
    [ComponentInteraction("blackjack-hit:*")]
    public async Task HitBlackJackAsync(string Id)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = GamblingService.GetBlackJackGame(Id);
        if (game?.Player.Id != Context.User.Id)
        {
            return;
        }

        var embed = ((SocketMessageComponent) Context.Interaction).Message.Embeds.First().ToEmbedBuilder();

        game.HitPlayer();
        var playerSum = game.GetPlayerSum();
        var dealerSum = game.GetDealerSum();
        await HandleGameStateAsync(game, embed, playerSum, dealerSum).ConfigureAwait(false);
    }

    [ComponentInteraction("blackjack-stand:*")]
    public async Task StandBlackJackAsync(string Id)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = GamblingService.GetBlackJackGame(Id);
        if (game?.Player.Id != Context.User.Id)
        {
            return;
        }

        var embed = ((SocketMessageComponent) Context.Interaction).Message.Embeds.First().ToEmbedBuilder();

        game.StandPlayer();
        var playerSum = game.GetPlayerSum();
        var dealerSum = game.GetDealerSum();
        await HandleGameStateAsync(game, embed, playerSum, dealerSum).ConfigureAwait(false);
    }

    private async Task HandleGameStateAsync(BlackJackGame game, EmbedBuilder embed, int playerSum, int dealerSum)
    {
        switch (game.State)
        {
            case GameState.Running:
            {
                embed.WithImageUrl(game.GetTablePicUrl());
                embed.Fields[0].Value = $"Érték: `{playerSum.ToString()}`";
                embed.Fields[1].Value = game.Hidden ? "Érték: `?`" : $"Érték: `{dealerSum.ToString()}`";
                await Context.Interaction.ModifyOriginalResponseAsync(x => x.Embed = embed.Build())
                    .ConfigureAwait(false);
                return;
            }
            case GameState.PlayerBust:
            {
                embed.WithDescription($"😭 Az osztó nyert! (PLAYER BUST)\n**{game.Stake}** 🪙KCoin-t veszítettél!");
                embed.WithImageUrl(game.GetTablePicUrl());
                embed.Fields[0].Value = $"Érték: `{playerSum.ToString()}`";
                embed.Fields[1].Value = game.Hidden ? "Érték: `?`" : $"Érték: `{dealerSum.ToString()}`";
                var dbUser = await Database.GetUserAsync(Context.Guild.Id, Context.User.Id).ConfigureAwait(false);
                dbUser.GamblingProfile.BlackJack.Losses--;
                await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
                await Context.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = embed.Build();
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                return;
            }
            case GameState.DealerBust:
            {
                embed.WithDescription($"🥳 A játékos nyert! (DEALER BUST)\n**{game.Stake}** 🪙KCoin-t szereztél!");
                embed.WithImageUrl(game.GetTablePicUrl());
                embed.Fields[0].Value = $"Érték: `{playerSum.ToString()}`";
                embed.Fields[1].Value = game.Hidden ? "Érték: `?`" : $"Érték: `{dealerSum.ToString()}`";
                var dbUser = await Database.GetUserAsync(Context.Guild.Id, Context.User.Id).ConfigureAwait(false);
                dbUser.GamblingProfile.Money += game.Stake;
                dbUser.GamblingProfile.BlackJack.Wins++;
                await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
                await Context.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = embed.Build();
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                return;
            }
            case GameState.PlayerBlackjack:
            {
                embed.WithDescription($"🥳 A játékos nyert! (BLACKJACK)\n**{game.Stake}** 🪙KCoin-t szereztél!");
                embed.WithImageUrl(game.GetTablePicUrl());
                embed.Fields[0].Value = $"Érték: `{playerSum.ToString()}`";
                embed.Fields[1].Value = game.Hidden ? "Érték: `?`" : $"Érték: `{dealerSum.ToString()}`";
                var dbUser = await Database.GetUserAsync(Context.Guild.Id, Context.User.Id).ConfigureAwait(false);
                dbUser.GamblingProfile.Money += game.Stake;
                dbUser.GamblingProfile.BlackJack.Wins++;
                await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
                await Context.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = embed.Build();
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                return;
            }
            case GameState.DealerBlackjack:
            {
                embed.WithDescription($"😭 Az osztó nyert! (BLACKJACK)\n**{game.Stake}** 🪙KCoin-t vesztettél!");
                embed.WithImageUrl(game.GetTablePicUrl());
                embed.Fields[0].Value = $"Érték: `{playerSum.ToString()}`";
                embed.Fields[1].Value = game.Hidden ? "Érték: `?`" : $"Érték: `{dealerSum.ToString()}`";
                var dbUser = await Database.GetUserAsync(Context.Guild.Id, Context.User.Id).ConfigureAwait(false);
                dbUser.GamblingProfile.BlackJack.Losses++;
                await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
                await Context.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = embed.Build();
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                return;
            }
            case GameState.PlayerWon:
            {
                embed.WithDescription($"🥳 A játékos nyert!\n**{game.Stake}** 🪙KCoin-t szereztél!");
                embed.WithImageUrl(game.GetTablePicUrl());
                embed.Fields[0].Value = $"Érték: `{playerSum.ToString()}`";
                embed.Fields[1].Value = game.Hidden ? "Érték: `?`" : $"Érték: `{dealerSum.ToString()}`";
                var dbUser = await Database.GetUserAsync(Context.Guild.Id, Context.User.Id).ConfigureAwait(false);
                dbUser.GamblingProfile.Money += game.Stake;
                dbUser.GamblingProfile.BlackJack.Wins++;
                await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
                await Context.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = embed.Build();
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                return;
            }
            case GameState.DealerWon:
            {
                embed.WithDescription($"😭 Az osztó nyert!\n**{game.Stake}** 🪙KCoin-t vesztettél!");
                embed.WithImageUrl(game.GetTablePicUrl());
                embed.Fields[0].Value = $"Érték: `{playerSum.ToString()}`";
                embed.Fields[1].Value = game.Hidden ? "Érték: `?`" : $"Érték: `{dealerSum.ToString()}`";
                var dbUser = await Database.GetUserAsync(Context.Guild.Id, Context.User.Id).ConfigureAwait(false);
                dbUser.GamblingProfile.BlackJack.Losses++;
                await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
                await Context.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = embed.Build();
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                return;
            }
            case GameState.Push:
            {
                embed.WithDescription("😕 Döntetlen! (PUSH)\n**A tét visszaadásra került!**");
                embed.WithImageUrl(game.GetTablePicUrl());
                embed.Fields[0].Value = $"Érték: `{playerSum.ToString()}`";
                embed.Fields[1].Value = game.Hidden ? "Érték: `?`" : $"Érték: `{dealerSum.ToString()}`";
                var dbUser = await Database.GetUserAsync(Context.Guild.Id, Context.User.Id).ConfigureAwait(false);
                dbUser.GamblingProfile.Money += game.Stake;
                await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
                await Context.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = embed.Build();
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                return;
            }
        }
    }
}