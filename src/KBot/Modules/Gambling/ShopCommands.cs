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
using KBot.Models.User;

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
    public async Task BuyLevelAsync([MinValue(1)] int levels)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await GetDbUser(Context.User).ConfigureAwait(false);
        var required = dbUser.MoneyToBuyLevel(levels);

        var eb = new EmbedBuilder()
            .WithTitle("Shop")
            .WithColor(Color.Gold)
            .WithDescription($"Balance: `{dbUser.Money.ToString()}`")
            .AddField("Levels", $"`{levels.ToString()}`", true)
            .AddField("Price", $"`{required.ToString()}`", true);

        if (dbUser.Money < required)
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

        await UpdateUserAsync(Context.User, x =>
        {
            x.Money -= required;
            x.Level += levels;
            x.Transactions.Add(new Transaction(id, TransactionType.ShopPurchase, -required, $"+{levels} levels"));
        }).ConfigureAwait(false);
        await UpdateUserAsync(BotUser, x => x.Money += required).ConfigureAwait(false);

        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = eb.WithDescription($"Successful purchase! 😎\nRemaining balance: `{dbUser.Money - required}`")
                .WithColor(Color.Green).Build();
            x.Components = new ComponentBuilder().Build();
        });
    }

    [SlashCommand("role", "Buy your own role")]
    public async Task BuyRoleAsync(string name, string hexcolor)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);

        var eb = new EmbedBuilder()
            .WithTitle("Shop")
            .WithColor(Color.Gold)
            .WithDescription($"Balance: `{dbUser.Money.ToString()}`")
            .AddField("Role", $"`{name}`", true)
            .AddField("Price", $"`{RolePrice.ToString()}`", true);

        if (dbUser.Money < RolePrice)
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

        await UpdateUserAsync(Context.User, x =>
        {
            x.Money -= RolePrice;
            x.Transactions.Add(
                new Transaction(id, TransactionType.ShopPurchase, -RolePrice, $"{role.Mention} role"));
            x.Roles.Add(role.Id);
        }).ConfigureAwait(false);
        await UpdateUserAsync(BotUser, x => x.Money += RolePrice).ConfigureAwait(false);

        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = eb.WithDescription($"Successful purchase! 😎\nRemaining Balance: `{dbUser.Money - RolePrice}`")
                .WithColor(Color.Green).Build();
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
    }

    [SlashCommand("category", "Buy your own category")]
    public async Task BuyCategoryAsync(string name)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);

        var eb = new EmbedBuilder()
            .WithTitle("Shop")
            .WithColor(Color.Gold)
            .WithDescription($"Balance: `{dbUser.Money.ToString()}`")
            .AddField("Category", $"`{name}`", true)
            .AddField("Price", $"`{CategoryPrice.ToString()}`", true);

        if (dbUser.Money < CategoryPrice)
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

        await UpdateUserAsync(Context.User, x =>
        {
            x.Money -= CategoryPrice;
            x.Transactions.Add(new Transaction(id, TransactionType.ShopPurchase, -CategoryPrice,
                $"{category.Name} category"));
        }).ConfigureAwait(false);
        await UpdateUserAsync(BotUser, x => x.Money += CategoryPrice).ConfigureAwait(false);

        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = eb
                .WithDescription($"Successful Purchase! 😎\nRemaining Balance: `{dbUser.Money - CategoryPrice}`")
                .WithColor(Color.Green).Build();
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
    }

    [SlashCommand("text", "Buy your own text channel")]
    public async Task BuyTextChannelAsync(string name)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);

        var eb = new EmbedBuilder()
            .WithTitle("Shop")
            .WithColor(Color.Gold)
            .WithDescription($"Balance: `{dbUser.Money.ToString()}`")
            .AddField("Csatorna", $"`{name}`", true)
            .AddField("Price", $"`{TextPrice.ToString()}`", true);

        if (dbUser.Money < TextPrice)
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

        await UpdateUserAsync(Context.User, x =>
        {
            x.Money -= TextPrice;
            x.Transactions.Add(new Transaction(id, TransactionType.ShopPurchase, -TextPrice,
                $"{channel.Mention} text channel"));
        }).ConfigureAwait(false);
        await UpdateUserAsync(BotUser, x => x.Money += TextPrice).ConfigureAwait(false);

        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = eb.WithDescription($"Successful Purchase! 😎\nRemaining Balance: `{dbUser.Money - TextPrice}`")
                .WithColor(Color.Green).Build();
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
    }

    [SlashCommand("voice", "Buy your own voice channel")]
    public async Task BuyVoiceChannelAsync(string name)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);

        var eb = new EmbedBuilder()
            .WithTitle("Shop")
            .WithColor(Color.Gold)
            .WithDescription($"Balance: `{dbUser.Money.ToString()}`")
            .AddField("Channel", $"`{name}`", true)
            .AddField("Price", $"`{VoicePrice.ToString()}`", true);

        if (dbUser.Money < VoicePrice)
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

        await UpdateUserAsync(Context.User, x =>
        {
            x.Money -= VoicePrice;
            x.Transactions.Add(new Transaction(id, TransactionType.ShopPurchase, -VoicePrice,
                $"{channel.Mention} voice channel"));
        }).ConfigureAwait(false);
        await UpdateUserAsync(BotUser, x => x.Money += VoicePrice).ConfigureAwait(false);

        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = eb.WithDescription($"Successful Purchase! 😎\nRemaining Balance: `{dbUser.Money - VoicePrice}`")
                .WithColor(Color.Green).Build();
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
    }

    [SlashCommand("kpack", "Buy everything")]
    public async Task BuyUltimateAsync([MinValue(1)] int levels, string roleName, string roleHexColor,
        string categoryName, string textName, string voiceName)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        var levelRequired = dbUser.MoneyToBuyLevel(levels);
        var required =
            (int) Math.Round((CategoryPrice + TextPrice + VoicePrice + RolePrice +
                              levelRequired) * 0.75);

        var eb = new EmbedBuilder()
            .WithTitle("Shop")
            .WithColor(Color.Gold)
            .WithDescription($"Balance: `{dbUser.Money.ToString()}`")
            .AddField("Choice",
                $"`+{levels.ToString()} level\n{roleName} role\n{categoryName} category\n{textName} text.\n{voiceName} voice.` ",
                true)
            .AddField("Price", $"`{required}`", true);

        if (dbUser.Money < required)
        {
            await FollowupAsync(embed: eb.WithDescription("Insufficient funds! 😭").Build()).ConfigureAwait(false);
            return;
        }

        var parsedSuccessfully = VerifyHexColorString(roleHexColor, out var color);
        if (!parsedSuccessfully)
        {
            await FollowupAsync(
                    embed: eb.WithDescription("Wrong hex code (try like this: #32a852, 32a852)! 😭").Build())
                .ConfigureAwait(false);
            return;
        }

        var id = Guid.NewGuid().ToString();

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
                x.Embed = eb.WithDescription("Time is up! 😭").WithColor(Color.Red).Build();
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);

        var category = await Context.Guild.CreateCategoryChannelAsync(categoryName, x =>
        {
            x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(new[]
            {
                new Overwrite(Context.Guild.EveryoneRole.Id, PermissionTarget.Role,
                    new OverwritePermissions(viewChannel: PermValue.Deny)),
                new Overwrite(Context.User.Id, PermissionTarget.User,
                    new OverwritePermissions(manageRoles: PermValue.Allow, viewChannel: PermValue.Allow))
            });
        }).ConfigureAwait(false);

        var role = await Context.Guild.CreateRoleAsync(roleName, GuildPermissions.None, new Color(color))
            .ConfigureAwait(false);
        await ((SocketGuildUser) Context.User).AddRoleAsync(role).ConfigureAwait(false);

        var text = await Context.Guild.CreateTextChannelAsync(textName, x =>
        {
            x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(new[]
            {
                new Overwrite(Context.Guild.EveryoneRole.Id, PermissionTarget.Role,
                    new OverwritePermissions(viewChannel: PermValue.Deny)),
                new Overwrite(Context.User.Id, PermissionTarget.User,
                    new OverwritePermissions(manageRoles: PermValue.Allow, viewChannel: PermValue.Allow))
            });
            x.CategoryId = category.Id;
        }).ConfigureAwait(false);

        var voice = await Context.Guild.CreateVoiceChannelAsync(voiceName, x =>
        {
            x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(new[]
            {
                new Overwrite(Context.Guild.EveryoneRole.Id, PermissionTarget.Role,
                    new OverwritePermissions(viewChannel: PermValue.Deny)),
                new Overwrite(Context.User.Id, PermissionTarget.User,
                    new OverwritePermissions(manageRoles: PermValue.Allow, viewChannel: PermValue.Allow))
            });
            x.CategoryId = category.Id;
        }).ConfigureAwait(false);

        await UpdateUserAsync(Context.User, x =>
        {
            x.Money -= required;
            x.Transactions.Add(new Transaction(id, TransactionType.ShopPurchase, -levelRequired,
                $"+{levels} level"));
            x.Transactions.Add(
                new Transaction(id, TransactionType.ShopPurchase, -11250000, $"{role.Mention} role"));
            x.Transactions.Add(new Transaction(id, TransactionType.ShopPurchase, -18750000,
                $"{category.Name} category"));
            x.Transactions.Add(new Transaction(id, TransactionType.ShopPurchase, -37500000,
                $"{text.Mention} text channel"));
            x.Transactions.Add(new Transaction(id, TransactionType.ShopPurchase, -37500000,
                $"{voice.Mention} voice channel"));
            x.Roles.Add(role.Id);
        }).ConfigureAwait(false);
        await UpdateUserAsync(BotUser, x => x.Money += required).ConfigureAwait(false);

        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = eb.WithDescription("Successful Purchase 😎!").WithColor(Color.Green).Build();
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