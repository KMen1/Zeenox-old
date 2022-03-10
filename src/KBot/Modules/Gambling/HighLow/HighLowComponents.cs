using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace KBot.Modules.Gambling.HighLow;

public class HighLowComponents : KBotModuleBase
{
    [ComponentInteraction("highlow-high:*")]
    public async Task HighLowHigh(string Id)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = GamblingService.GetHighLowGame(Id);
        if (game?.Player.Id != Context.User.Id)
        {
            return;
        }

        var components = new ComponentBuilder().Build();
        var embed = ((SocketMessageComponent) Context.Interaction).Message.Embeds.First().ToEmbedBuilder();
        if (game.High())
        {
            var imgUrl = game.Draw();
            embed.WithDescription($"Tét: **{game.Stake} kredit**");
            embed.Fields[0].Value = $"Szorzó: **{game.HighMultiplier.ToString()}**" +
                                    $"\nNyeremény: **{game.HighStake.ToString()} kredit**";
            embed.Fields[1].Value = $"Szorzó: **{game.LowMultiplier.ToString()}**\n" +
                                    $"Nyeremény: **{game.LowStake.ToString()}** kredit";
            embed.WithImageUrl(imgUrl);
            components = new ComponentBuilder()
                .WithButton(" ", $"highlow-high:{game.Id}", emote: new Emoji("⬆"))
                .WithButton(" ", $"highlow-low:{game.Id}", emote: new Emoji("⬇"))
                .WithButton(" ", $"highlow-finish:{game.Id}", emote: new Emoji("❌"), disabled:false)
                .Build();
        }
        else
        {
            embed.WithDescription($"Nem találtad el! Vesztettél **{game.Stake}** kreditet!");
            embed.WithImageUrl(game.Reveal());
            GamblingService.RemoveHighLowGame(Id);
            var dbUser = await Database.GetUserAsync(Context.Guild.Id, Context.User.Id).ConfigureAwait(false);
            dbUser.GamblingProfile.HighLow.MoneyLost += game.Stake;
            dbUser.GamblingProfile.HighLow.Losses++;
            await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
        }

        await Context.Interaction.ModifyOriginalResponseAsync(x =>
        {
            x.Embed = embed.Build();
            x.Components = components;
        }).ConfigureAwait(false);
    }
    [ComponentInteraction("highlow-low:*")]
    public async Task HighLowLow(string Id)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = GamblingService.GetHighLowGame(Id);
        if (game?.Player.Id != Context.User.Id)
        {
            return;
        }

        var embed = ((SocketMessageComponent) Context.Interaction).Message.Embeds.First().ToEmbedBuilder();

        var components = new ComponentBuilder().Build();
        if (game.Low())
        {
            var imgUrl = game.Draw();
            embed.WithDescription($"Tét: **{game.Stake} kredit**");
            embed.Fields[0].Value = $"Szorzó: **{game.HighMultiplier.ToString()}**" +
                                    $"\nNyeremény: **{game.HighStake.ToString()} kredit**";
            embed.Fields[1].Value = $"Szorzó: **{game.LowMultiplier.ToString()}**\n" +
                                    $"Nyeremény: **{game.LowStake.ToString()}** kredit";
            embed.WithImageUrl(imgUrl);
            components = new ComponentBuilder()
                .WithButton(" ", $"highlow-high:{game.Id}", emote: new Emoji("⬆"))
                .WithButton(" ", $"highlow-low:{game.Id}", emote: new Emoji("⬇"))
                .WithButton(" ", $"highlow-finish:{game.Id}", emote: new Emoji("❌"), disabled:false)
                .Build();
        }
        else
        {
            embed.WithDescription($"Nem találtad el! Vesztettél **{game.Stake}** kreditet!");
            embed.WithImageUrl(game.Reveal());
            GamblingService.RemoveHighLowGame(Id);
            var dbUser = await Database.GetUserAsync(Context.Guild.Id, Context.User.Id).ConfigureAwait(false);
            dbUser.GamblingProfile.HighLow.MoneyLost += game.Stake;
            dbUser.GamblingProfile.HighLow.Losses++;
            await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
        }

        await Context.Interaction.ModifyOriginalResponseAsync(x =>
        {
            x.Embed = embed.Build();
            x.Components = components;
        }).ConfigureAwait(false);
    }
    [ComponentInteraction("highlow-finish:*")]
    public async Task HighLowFinish(string Id)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = GamblingService.GetHighLowGame(Id);
        if (game?.Player.Id != Context.User.Id)
        {
            return;
        }

        var embed = ((SocketMessageComponent) Context.Interaction).Message.Embeds.First().ToEmbedBuilder();
        embed.WithDescription($"A játék véget ért! **{game.Stake}** kreditet szereztél!");
        embed.WithImageUrl(game.Reveal());
        GamblingService.RemoveHighLowGame(Id);
        var dbUser = await Database.GetUserAsync(Context.Guild.Id, Context.User.Id).ConfigureAwait(false);
        dbUser.GamblingProfile.Money += game.Stake;
        dbUser.GamblingProfile.HighLow.MoneyWon += game.Stake;
        dbUser.GamblingProfile.HighLow.Wins++;
        await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
        await Context.Interaction.ModifyOriginalResponseAsync(x =>
        {
            x.Embed = embed.Build();
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
    }
}