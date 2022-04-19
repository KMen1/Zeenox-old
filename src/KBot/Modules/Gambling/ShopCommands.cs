using System;
using System.Collections.Generic;
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
    private const int CategoryPrice = 150000000;
    private const int VoicePrice = 200000000;
    private const int TextPrice = 175000000;
    private const int RolePrice = 125000000;

    public InteractiveService Interactive { get; set; }

    [SlashCommand("level", "Buy extra levels")]
    public async Task BuyLevelAsync([MinValue(1), MaxValue(10)] int amount)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)Context.User).ConfigureAwait(false);
        var required = dbUser.MoneyToBuyLevel(amount);

        var eb = new EmbedBuilder()
            .WithTitle("Shop")
            .WithColor(Color.Gold)
            .WithDescription($"Balance: `{dbUser.Balance.ToString()}`")
            .AddField("Levels", $"`{amount.ToString()}`", true)
            .AddField("Price", $"`{required.ToString()}`", true);

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

        var result = await Interactive
            .NextMessageComponentAsync(x => x.Data.CustomId == $"shop-buy:{id}", timeout: TimeSpan.FromMinutes(1))
            .ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = eb.WithDescription("Time is up!").WithColor(Color.Red).Build();
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);
            return;
        }

        await Mongo.AddTransactionAsync(new Transaction(
            id,
            TransactionType.LevelPurchase,
            -required,
            $"{amount}"), (SocketGuildUser)Context.User).ConfigureAwait(false);
        await Mongo.UpdateUserAsync((SocketGuildUser)Context.User, x =>
        {
            x.Balance -= required;
            x.Level += amount;
        }).ConfigureAwait(false);

        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = eb.WithDescription($"Successful purchase! 😎\nRemaining balance: `{dbUser.Balance - required}`")
                .WithColor(Color.Green).Build();
            x.Components = new ComponentBuilder().Build();
        });
    }

    [SlashCommand("role", "Buy your own role")]
    public async Task BuyRoleAsync(string name, string hexcolor)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)Context.User).ConfigureAwait(false);

        var eb = new EmbedBuilder()
            .WithTitle("Shop")
            .WithColor(Color.Gold)
            .WithDescription($"Balance: `{dbUser.Balance.ToString()}`")
            .AddField("Role", $"`{name}`", true)
            .AddField("Price", $"`{RolePrice.ToString()}`", true);

        if (dbUser.Balance < RolePrice)
        {
            await FollowupAsync(embed: eb.WithDescription("Insufficient Funds! 😭").Build()).ConfigureAwait(false);
            return;
        }

        var parsedSuccessfully = VerifyHexColorString(hexcolor, out var color);
        if (!parsedSuccessfully)
        {
            await FollowupAsync(
                    embed: eb.WithDescription("Wrong hex code (try like this: #32a852, 32a852)! 😭").Build())
                .ConfigureAwait(false);
            return;
        }

        var id = Guid.NewGuid().ToShortId();

        var comp = new ComponentBuilder()
            .WithButton("Buy", $"shop-buy:{id}", ButtonStyle.Success, new Emoji("🛒"))
            .Build();

        await FollowupAsync(embed: eb.Build(), components: comp).ConfigureAwait(false);

        var result = await Interactive
            .NextMessageComponentAsync(x => x.Data.CustomId == $"shop-buy:{id}", timeout: TimeSpan.FromMinutes(1))
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = eb.WithDescription("Time is up!").WithColor(Color.Red).Build();
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);
            return;
        }

        var role = await Context.Guild.CreateRoleAsync(name, GuildPermissions.None, new Color(color))
            .ConfigureAwait(false);
        await ((SocketGuildUser) Context.User).AddRoleAsync(role).ConfigureAwait(false);

        
        await Mongo.AddTransactionAsync(new Transaction(
            id,
            TransactionType.RolePurchase,
            -RolePrice,
            role.Mention), (SocketGuildUser)Context.User).ConfigureAwait(false);
        await Mongo.UpdateUserAsync((SocketGuildUser)Context.User, x =>
        {
            x.Balance -= RolePrice;
            x.Roles.Add(role.Id);
        }).ConfigureAwait(false);

        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = eb.WithDescription($"Successful purchase! 😎\nRemaining Balance: `{dbUser.Balance - RolePrice}`")
                .WithColor(Color.Green).Build();
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
    }

    [SlashCommand("category", "Buy your own category")]
    public async Task BuyCategoryAsync(string name)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)Context.User).ConfigureAwait(false);

        var eb = new EmbedBuilder()
            .WithTitle("Shop")
            .WithColor(Color.Gold)
            .WithDescription($"Balance: `{dbUser.Balance.ToString()}`")
            .AddField("Category", $"`{name}`", true)
            .AddField("Price", $"`{CategoryPrice.ToString()}`", true);

        if (dbUser.Balance < CategoryPrice)
        {
            await FollowupAsync(embed: eb.WithDescription("Insufficient funds! 😭").Build()).ConfigureAwait(false);
            return;
        }

        var id = Guid.NewGuid().ToShortId();

        var comp = new ComponentBuilder()
            .WithButton("Buy", $"shop-buy:{id}", ButtonStyle.Success, new Emoji("🛒"))
            .Build();

        await FollowupAsync(embed: eb.Build(), components: comp).ConfigureAwait(false);

        var result = await Interactive
            .NextMessageComponentAsync(x => x.Data.CustomId == $"shop-buy:{id}", timeout: TimeSpan.FromMinutes(1))
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = eb.WithDescription("Time is up!").WithColor(Color.Red).Build();
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);
            return;
        }

        var category = await Context.Guild.CreateCategoryChannelAsync(name, x =>
        {
            x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(new[]
            {
                new Overwrite(Context.Guild.EveryoneRole.Id, PermissionTarget.Role,
                    new OverwritePermissions(viewChannel: PermValue.Deny)),
                new Overwrite(Context.User.Id, PermissionTarget.User,
                    new OverwritePermissions(manageRoles: PermValue.Allow, viewChannel: PermValue.Allow))
            });
        }).ConfigureAwait(false);

        await Mongo.AddTransactionAsync(new Transaction(
            id,
            TransactionType.CategoryPurchase,
            -CategoryPrice,
            category.Name), (SocketGuildUser)Context.User).ConfigureAwait(false);
        await Mongo.UpdateUserAsync((SocketGuildUser)Context.User, x => x.Balance -= CategoryPrice).ConfigureAwait(false);

        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = eb
                .WithDescription($"Successful Purchase! 😎\nRemaining Balance: `{dbUser.Balance - CategoryPrice}`")
                .WithColor(Color.Green).Build();
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
    }

    [SlashCommand("text", "Buy your own text channel")]
    public async Task BuyTextChannelAsync(string name)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)Context.User).ConfigureAwait(false);

        var eb = new EmbedBuilder()
            .WithTitle("Shop")
            .WithColor(Color.Gold)
            .WithDescription($"Balance: `{dbUser.Balance.ToString()}`")
            .AddField("Csatorna", $"`{name}`", true)
            .AddField("Price", $"`{TextPrice.ToString()}`", true);

        if (dbUser.Balance < TextPrice)
        {
            await FollowupAsync(embed: eb.WithDescription("Insufficient funds! 😭").Build()).ConfigureAwait(false);
            return;
        }

        var id = Guid.NewGuid().ToShortId();

        var comp = new ComponentBuilder()
            .WithButton("Buy", $"shop-buy:{id}", ButtonStyle.Success, new Emoji("🛒"))
            .Build();

        await FollowupAsync(embed: eb.Build(), components: comp).ConfigureAwait(false);

        var result = await Interactive
            .NextMessageComponentAsync(x => x.Data.CustomId == $"shop-buy:{id}", timeout: TimeSpan.FromMinutes(1))
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = eb.WithDescription("Time is up!").WithColor(Color.Red).Build();
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);
            return;
        }

        var channel = await Context.Guild.CreateTextChannelAsync(name, x =>
        {
            x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(new[]
            {
                new Overwrite(Context.Guild.EveryoneRole.Id, PermissionTarget.Role,
                    new OverwritePermissions(viewChannel: PermValue.Deny)),
                new Overwrite(Context.User.Id, PermissionTarget.User,
                    new OverwritePermissions(manageRoles: PermValue.Allow, viewChannel: PermValue.Allow))
            });
        }).ConfigureAwait(false);

        await Mongo.AddTransactionAsync(new Transaction(
            id,
            TransactionType.TextPurchase,
            -TextPrice,
            channel.Mention), (SocketGuildUser)Context.User).ConfigureAwait(false);
        await Mongo.UpdateUserAsync((SocketGuildUser)Context.User, x => x.Balance -= TextPrice).ConfigureAwait(false);

        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = eb.WithDescription($"Successful Purchase! 😎\nRemaining Balance: `{dbUser.Balance - TextPrice}`")
                .WithColor(Color.Green).Build();
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
    }

    [SlashCommand("voice", "Buy your own voice channel")]
    public async Task BuyVoiceChannelAsync(string name)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)Context.User).ConfigureAwait(false);

        var eb = new EmbedBuilder()
            .WithTitle("Shop")
            .WithColor(Color.Gold)
            .WithDescription($"Balance: `{dbUser.Balance.ToString()}`")
            .AddField("Channel", $"`{name}`", true)
            .AddField("Price", $"`{VoicePrice.ToString()}`", true);

        if (dbUser.Balance < VoicePrice)
        {
            await FollowupAsync(embed: eb.WithDescription("Insufficient funds! 😭").Build()).ConfigureAwait(false);
            return;
        }

        var id = Guid.NewGuid().ToShortId();

        var comp = new ComponentBuilder()
            .WithButton("Buy", $"shop-buy:{id}", ButtonStyle.Success, new Emoji("🛒"))
            .Build();

        await FollowupAsync(embed: eb.Build(), components: comp).ConfigureAwait(false);

        var result = await Interactive
            .NextMessageComponentAsync(x => x.Data.CustomId == $"shop-buy:{id}", timeout: TimeSpan.FromMinutes(1))
            .ConfigureAwait(false);

        if (!result.IsSuccess)
            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = eb.WithDescription("Time is up!").WithColor(Color.Red).Build();
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);

        var channel = await Context.Guild.CreateVoiceChannelAsync(name, x =>
        {
            x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(new[]
            {
                new Overwrite(Context.Guild.EveryoneRole.Id, PermissionTarget.Role,
                    new OverwritePermissions(viewChannel: PermValue.Deny)),
                new Overwrite(Context.User.Id, PermissionTarget.User,
                    new OverwritePermissions(manageRoles: PermValue.Allow, viewChannel: PermValue.Allow))
            });
        }).ConfigureAwait(false);

        await Mongo.AddTransactionAsync(new Transaction(
            id,
            TransactionType.VoicePurchase,
            -VoicePrice,
            channel.Mention), (SocketGuildUser)Context.User).ConfigureAwait(false);
        await Mongo.UpdateUserAsync((SocketGuildUser)Context.User, x => x.Balance -= VoicePrice).ConfigureAwait(false);

        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = eb.WithDescription($"Successful Purchase! 😎\nRemaining Balance: `{dbUser.Balance - VoicePrice}`")
                .WithColor(Color.Green).Build();
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
    }

    private static bool VerifyHexColorString(string hexcolor, out uint color)
    {
        if (hexcolor.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase) ||
            hexcolor.StartsWith("&H", StringComparison.CurrentCultureIgnoreCase))
            hexcolor = hexcolor[2..];

        if (hexcolor.StartsWith("#", StringComparison.CurrentCultureIgnoreCase)) hexcolor = hexcolor[1..];

        var parsedSuccessfully = uint.TryParse(hexcolor, NumberStyles.HexNumber, CultureInfo.CurrentCulture,
            out color);
        return parsedSuccessfully;
    }
}