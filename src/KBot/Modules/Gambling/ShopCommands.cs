using System;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using KBot.Enums;
using KBot.Extensions;
using KBot.Models;

namespace KBot.Modules.Gambling;

[Group("shop", "Stuff to buy with your money")]
public class ShopCommands : SlashModuleBase
{
    private const int RolePrice = 100000000;
    private readonly InteractiveService _interactive;

    public ShopCommands(InteractiveService interactive)
    {
        _interactive = interactive;
    }

    [SlashCommand("level", "Buy one level")]
    public async Task BuyLevelAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)Context.User).ConfigureAwait(false);
        var required = dbUser.MoneyToBuyNextLevel;

        var eb = new EmbedBuilder()
            .WithTitle("Shop")
            .WithColor(Color.Gold)
            .WithDescription(
                $"Balance: `{dbUser.Balance.ToString("N0", CultureInfo.InvariantCulture)}`"
            )
            .AddField("Price", $"`{required.ToString("N0", CultureInfo.InvariantCulture)}`", true);

        if (dbUser.Balance < required)
        {
            eb.WithDescription("Insufficient funds! 😭");
            await FollowupAsync(embed: eb.Build()).ConfigureAwait(false);
            return;
        }

        var id = Guid.NewGuid().ToShortId();
        var comp = new ComponentBuilder()
            .WithButton("Buy", $"shop-buy:{id}", ButtonStyle.Success, new Emoji("🛒"))
            .Build();

        await FollowupAsync(embed: eb.Build(), components: comp).ConfigureAwait(false);

        var result = await _interactive
            .NextMessageComponentAsync(
                x => x.Data.CustomId.Equals($"shop-buy:{id}", StringComparison.OrdinalIgnoreCase),
                timeout: TimeSpan.FromMinutes(1)
            )
            .ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            await ModifyOriginalResponseAsync(
                    x =>
                    {
                        x.Embed = eb.WithDescription("Time is up!").WithColor(Color.Red).Build();
                        x.Components = new ComponentBuilder().Build();
                    }
                )
                .ConfigureAwait(false);
            return;
        }

        await Mongo
            .AddTransactionAsync(
                new Transaction(id, TransactionType.LevelPurchase, -required),
                (SocketGuildUser)Context.User
            )
            .ConfigureAwait(false);
        await Mongo
            .UpdateUserAsync(
                (SocketGuildUser)Context.User,
                x =>
                {
                    x.Balance -= required;
                    x.Level++;
                }
            )
            .ConfigureAwait(false);

        await ModifyOriginalResponseAsync(
                x =>
                {
                    x.Embed = eb.WithDescription(
                            $"Successful purchase! 😎\nRemaining balance: `{dbUser.Balance - required}`"
                        )
                        .WithColor(Color.Green)
                        .Build();
                    x.Components = new ComponentBuilder().Build();
                }
            )
            .ConfigureAwait(false);
    }

    [SlashCommand("role", "Buy your own role")]
    public async Task BuyRoleAsync(string name, string hexcolor)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)Context.User).ConfigureAwait(false);

        var eb = new EmbedBuilder()
            .WithTitle("Shop")
            .WithColor(Color.Gold)
            .WithDescription(
                $"Balance: `{dbUser.Balance.ToString("N0", CultureInfo.InvariantCulture)}`"
            )
            .AddField("Role", $"`{name}`", true)
            .AddField("Price", $"`{RolePrice.ToString("N0", CultureInfo.InvariantCulture)}`", true);

        if (dbUser.Balance < RolePrice)
        {
            await FollowupAsync(embed: eb.WithDescription("Insufficient Funds! 😭").Build())
                .ConfigureAwait(false);
            return;
        }

        var parsedSuccessfully = VerifyHexColorString(hexcolor, out var color);
        if (!parsedSuccessfully)
        {
            await FollowupAsync(
                    embed: eb.WithDescription("Wrong hex code (try like this: #32a852, 32a852)! 😭")
                        .Build()
                )
                .ConfigureAwait(false);
            return;
        }

        var id = Guid.NewGuid().ToShortId();

        var comp = new ComponentBuilder()
            .WithButton("Buy", $"shop-buy:{id}", ButtonStyle.Success, new Emoji("🛒"))
            .Build();

        await FollowupAsync(embed: eb.Build(), components: comp).ConfigureAwait(false);

        var result = await _interactive
            .NextMessageComponentAsync(
                x => x.Data.CustomId.Equals($"shop-buy:{id}", StringComparison.OrdinalIgnoreCase),
                timeout: TimeSpan.FromMinutes(1)
            )
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await ModifyOriginalResponseAsync(
                    x =>
                    {
                        x.Embed = eb.WithDescription("Time is up!").WithColor(Color.Red).Build();
                        x.Components = new ComponentBuilder().Build();
                    }
                )
                .ConfigureAwait(false);
            return;
        }

        var role = await Context.Guild
            .CreateRoleAsync(name, GuildPermissions.None, new Color(color))
            .ConfigureAwait(false);
        await ((SocketGuildUser)Context.User).AddRoleAsync(role).ConfigureAwait(false);

        await Mongo
            .AddTransactionAsync(
                new Transaction(id, TransactionType.RolePurchase, -RolePrice, role.Mention),
                (SocketGuildUser)Context.User
            )
            .ConfigureAwait(false);
        await Mongo
            .UpdateUserAsync(
                (SocketGuildUser)Context.User,
                x =>
                {
                    x.Balance -= RolePrice;
                    x.Roles.Add(role.Id);
                }
            )
            .ConfigureAwait(false);

        await ModifyOriginalResponseAsync(
                x =>
                {
                    x.Embed = eb.WithDescription(
                            $"Successful purchase! 😎\nRemaining Balance: `{dbUser.Balance - RolePrice}`"
                        )
                        .WithColor(Color.Green)
                        .Build();
                    x.Components = new ComponentBuilder().Build();
                }
            )
            .ConfigureAwait(false);
    }

    private static bool VerifyHexColorString(string hexcolor, out uint color)
    {
        if (
            hexcolor.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase)
            || hexcolor.StartsWith("&H", StringComparison.CurrentCultureIgnoreCase)
        )
            hexcolor = hexcolor[2..];

        if (hexcolor.StartsWith("#", StringComparison.CurrentCultureIgnoreCase))
            hexcolor = hexcolor[1..];

        var parsedSuccessfully = uint.TryParse(
            hexcolor,
            NumberStyles.HexNumber,
            CultureInfo.CurrentCulture,
            out color
        );
        return parsedSuccessfully;
    }
}
