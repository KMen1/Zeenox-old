using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using KBot.Enums;
using KBot.Models;

namespace KBot.Modules.Gambling;
[Group("shop", "Szerencsejáték piac")]
public class ShopCommands : KBotModuleBase
{
    private const int CategoryPrice = 150000000;
    private const int VoicePrice = 200000000;
    private const int TextPrice = 175000000;
    private const int RolePrice = 125000000;

    public InteractiveService Interactive { get; set; }

    [SlashCommand("level", "Extra szint vásárlása")]
    public async Task BuyLevelAsync([MinValue(1)] int levels)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await GetDbUser(Context.User).ConfigureAwait(false);
        var required = dbUser.MoneyToBuyLevel(levels);

        var eb = new EmbedBuilder()
            .WithTitle("Bolt")
            .WithColor(Color.Gold)
            .WithDescription($"Egyenleg: `{dbUser.Money.ToString()}`")
            .AddField("Kiválasztva", $"`+{levels.ToString()} Szint`", true)
            .AddField("Összeg", $"`{required.ToString()}`", true);

        if (dbUser.Money < required)
        {
            eb.WithDescription("Nincs elég pénzed! 😭");
            await FollowupAsync(embed: eb.Build()).ConfigureAwait(false);
            return;
        }

        var id = Guid.NewGuid().ToShortId();
        var comp = new ComponentBuilder()
            .WithButton("Vásárlás", $"shop-buy:{id}", ButtonStyle.Success, new Emoji("🛒"))
            .Build();
        
        await FollowupAsync(embed: eb.Build(), components: comp).ConfigureAwait(false);

        var result = await Interactive
            .NextMessageComponentAsync(x => x.Data.CustomId == $"shop-buy:{id}", timeout: TimeSpan.FromMinutes(1))
            .ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = eb.WithDescription("Lejárt az idő!").WithColor(Color.Red).Build();
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);
            return;
        }

        await UpdateUserAsync(Context.User, x =>
        {
            x.Money -= required;
            x.Level += levels;
            x.Transactions.Add(new Transaction(id, TransactionType.ShopPurchase, -required, $"+{levels} szint"));
        }).ConfigureAwait(false);
        await UpdateUserAsync(BotUser, x => x.Money += required).ConfigureAwait(false);

        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = eb.WithDescription($"Sikeres vásárlás! 😎\nMegmaradt egyenleg: `{dbUser.Money - required}`")
                .WithColor(Color.Green).Build();
            x.Components = new ComponentBuilder().Build();
        });
    }

    [SlashCommand("role", "Saját rang vásárlása")]
    public async Task BuyRoleAsync(string name, string hexcolor)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);

        var eb = new EmbedBuilder()
            .WithTitle("Bolt")
            .WithColor(Color.Gold)
            .WithDescription($"Egyenleg: `{dbUser.Money.ToString()}`")
            .AddField("Kiválasztva", $"`{name} rang`", true)
            .AddField("Összeg", $"`{RolePrice.ToString()}`", true);
        
        if (dbUser.Money < RolePrice)
        {
            await FollowupAsync(embed: eb.WithDescription("Nincs elég pénzed! 😭").Build()).ConfigureAwait(false);
            return;
        }

        var parsedSuccessfully = VerifyHexColorString(hexcolor, out var color);
        if (!parsedSuccessfully)
        {
            await FollowupAsync(embed: eb.WithDescription("Hibás hex színkód (ilyen formákban adhatod meg: #32a852, 32a852)! 😭").Build()).ConfigureAwait(false);
            return;
        }
        
        var id = Guid.NewGuid().ToShortId();

        var comp = new ComponentBuilder()
            .WithButton("Vásárlás", $"shop-buy:{id}", ButtonStyle.Success, new Emoji("🛒"))
            .Build();
        
        await FollowupAsync(embed: eb.Build(), components: comp).ConfigureAwait(false);
        
        var result = await Interactive
            .NextMessageComponentAsync(x => x.Data.CustomId == $"shop-buy:{id}", timeout: TimeSpan.FromMinutes(1))
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = eb.WithDescription("Lejárt az idő!").WithColor(Color.Red).Build();
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
                new Transaction(id, TransactionType.ShopPurchase, -RolePrice, $"{role.Mention} rang"));
            x.Roles.Add(role.Id);
        }).ConfigureAwait(false);
        await UpdateUserAsync(BotUser, x => x.Money += RolePrice).ConfigureAwait(false);

        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = eb.WithDescription($"Sikeres vásárlás! 😎\nMegmaradt egyenleg: `{dbUser.Money - RolePrice}`")
                .WithColor(Color.Green).Build();
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
    }

    [SlashCommand("category", "Saját kategória vásárlása")]
    public async Task BuyCategoryAsync(string name)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        
        var eb = new EmbedBuilder()
            .WithTitle("Bolt")
            .WithColor(Color.Gold)
            .WithDescription($"Egyenleg: `{dbUser.Money.ToString()}`")
            .AddField("Kiválasztva", $"`{name} kategória`", true)
            .AddField("Összeg", $"`{CategoryPrice.ToString()}`", true);
        
        if (dbUser.Money < CategoryPrice)
        {
            await FollowupAsync(embed: eb.WithDescription("Nincs elég pénzed! 😭").Build()).ConfigureAwait(false);
            return;
        }

        if (dbUser.BoughtChannels.Exists(x => x.Type == DiscordChannelType.Category))
        {
            await FollowupAsync(embed: eb.WithDescription("Már vásároltál egy kategóriát! 😭").Build()).ConfigureAwait(false);
            return;
        }

        var id = Guid.NewGuid().ToShortId();
        
        var comp = new ComponentBuilder()
            .WithButton("Vásárlás", $"shop-buy:{id}", ButtonStyle.Success, new Emoji("🛒"))
            .Build();
        
        await FollowupAsync(embed: eb.Build(), components: comp).ConfigureAwait(false);
        
        var result = await Interactive
            .NextMessageComponentAsync(x => x.Data.CustomId == $"shop-buy:{id}", timeout: TimeSpan.FromMinutes(1))
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = eb.WithDescription("Lejárt az idő!").WithColor(Color.Red).Build();
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
                $"{category.Name} kategória"));
            x.BoughtChannels.Add(new DiscordChannel(category.Id, DiscordChannelType.Category));
        }).ConfigureAwait(false);
        await UpdateUserAsync(BotUser, x => x.Money += CategoryPrice).ConfigureAwait(false);
        
        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = eb.WithDescription($"Sikeres vásárlás! 😎\nMegmaradt egyenleg: `{dbUser.Money - CategoryPrice}`")
                .WithColor(Color.Green).Build();
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
    }

    [SlashCommand("text", "Saját szövegcsatorna vásárlása")]
    public async Task BuyTextChannelAsync(string name)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        
        var eb = new EmbedBuilder()
            .WithTitle("Bolt")
            .WithColor(Color.Gold)
            .WithDescription($"Egyenleg: `{dbUser.Money.ToString()}`")
            .AddField("Kiválasztva", $"`{name} szövegcsatorna`", true)
            .AddField("Összeg", $"`{TextPrice.ToString()}`", true);
        
        if (dbUser.Money < TextPrice)
        {
            await FollowupAsync(embed: eb.WithDescription("Nincs elég pénzed! 😭").Build()).ConfigureAwait(false);
            return;
        }

        var category = dbUser.BoughtChannels.Find(x => x.Type == DiscordChannelType.Category);
        if (category is null)
        {
            await FollowupAsync(embed: eb.WithDescription("Nem vásároltál kategóriát! 😭").Build()).ConfigureAwait(false);
            return;
        }
        
        var id = Guid.NewGuid().ToShortId();
        
        var comp = new ComponentBuilder()
            .WithButton("Vásárlás", $"shop-buy:{id}", ButtonStyle.Success, new Emoji("🛒"))
            .Build();
        
        await FollowupAsync(embed: eb.Build(), components: comp).ConfigureAwait(false);
        
        var result = await Interactive
            .NextMessageComponentAsync(x => x.Data.CustomId == $"shop-buy:{id}", timeout: TimeSpan.FromMinutes(1))
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = eb.WithDescription("Lejárt az idő!").WithColor(Color.Red).Build();
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
            x.CategoryId = category.Id;
        }).ConfigureAwait(false);
        
        await UpdateUserAsync(Context.User, x =>
        {
            x.Money -= TextPrice;
            x.Transactions.Add(new Transaction(id, TransactionType.ShopPurchase, -TextPrice,
                $"{channel.Mention} szövegcsatorna"));
            x.BoughtChannels.Add(new DiscordChannel(channel.Id, DiscordChannelType.Text));
        }).ConfigureAwait(false);
        await UpdateUserAsync(BotUser, x => x.Money += TextPrice).ConfigureAwait(false);
        
        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = eb.WithDescription($"Sikeres vásárlás! 😎\nMegmaradt egyenleg: `{dbUser.Money - TextPrice}`")
                .WithColor(Color.Green).Build();
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
    }

    [SlashCommand("voice", "Saját hangcsatorna vásárlása")]
    public async Task BuyVoiceChannelAsync(string name)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        
        var eb = new EmbedBuilder()
            .WithTitle("Bolt")
            .WithColor(Color.Gold)
            .WithDescription($"Egyenleg: `{dbUser.Money.ToString()}`")
            .AddField("Kiválasztva", $"`{name} hangcsatorna`", true)
            .AddField("Összeg", $"`{VoicePrice.ToString()}`", true);
        
        if (dbUser.Money < VoicePrice)
        {
            await FollowupAsync(embed: eb.WithDescription("Nincs elég pénzed! 😭").Build()).ConfigureAwait(false);
            return;
        }

        var category = dbUser.BoughtChannels.Find(x => x.Type == DiscordChannelType.Category);
        if (category is null)
        {
            await FollowupAsync(embed: eb.WithDescription("Nem vásároltál kategóriát! 😭").Build()).ConfigureAwait(false);
            return;
        }
        
        var id = Guid.NewGuid().ToShortId();
        
        var comp = new ComponentBuilder()
            .WithButton("Vásárlás", $"shop-buy:{id}", ButtonStyle.Success, new Emoji("🛒"))
            .Build();
        
        await FollowupAsync(embed: eb.Build(), components: comp).ConfigureAwait(false);
        
        var result = await Interactive
            .NextMessageComponentAsync(x => x.Data.CustomId == $"shop-buy:{id}", timeout: TimeSpan.FromMinutes(1))
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = eb.WithDescription("Lejárt az idő!").WithColor(Color.Red).Build();
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);
        }

        var channel = await Context.Guild.CreateVoiceChannelAsync(name, x =>
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
            x.Money -= VoicePrice;
            x.Transactions.Add(new Transaction(id, TransactionType.ShopPurchase, -VoicePrice,
                $"{channel.Mention} hangcsatorna"));
            x.BoughtChannels.Add(new DiscordChannel(channel.Id, DiscordChannelType.Voice));
        }).ConfigureAwait(false);
        await UpdateUserAsync(BotUser, x => x.Money += VoicePrice).ConfigureAwait(false);
        
        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = eb.WithDescription($"Sikeres vásárlás! 😎\nMegmaradt egyenleg: `{dbUser.Money - VoicePrice}`")
                .WithColor(Color.Green).Build();
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
    }

    [SlashCommand("kpack", "Minden vásárlása akciós csomagként")]
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
            .WithTitle("Bolt")
            .WithColor(Color.Gold)
            .WithDescription($"Egyenleg: `{dbUser.Money.ToString()}`")
            .AddField("Kiválasztva", $"`+{levels.ToString()} szint\n{roleName} rang\n{categoryName} kategória\n{textName} szövegcs.\n{voiceName} hangcs.` ", true)
            .AddField("Összeg", $"`{required}`", true);
        
        if (dbUser.Money < required)
        {
            await FollowupAsync(embed: eb.WithDescription("Nincs elég pénzed! 😭").Build()).ConfigureAwait(false);
            return;
        }

        if (dbUser.BoughtChannels.Exists(x => x.Type == DiscordChannelType.Category))
        {
            await FollowupAsync(embed: eb.WithDescription("Már vásároltál kategóriát ezért nem válthatod be! 😭").Build()).ConfigureAwait(false);
            return;
        }

        var parsedSuccessfully = VerifyHexColorString(roleHexColor, out var color);
        if (!parsedSuccessfully)
        {
            await FollowupAsync(embed: eb.WithDescription("Hibás hex színkód (ilyen formákban adhatod meg: #32a852, 32a852)! 😭").Build()).ConfigureAwait(false);
            return;
        }
        
        var id = Guid.NewGuid().ToString();

        var comp = new ComponentBuilder()
            .WithButton("Vásárlás", $"shop-buy:{id}", ButtonStyle.Success, new Emoji("🛒"))
            .Build();

        var result = await Interactive
            .NextMessageComponentAsync(x => x.Data.CustomId == $"shop-buy:{id}", timeout: TimeSpan.FromMinutes(1))
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = eb.WithDescription("Lejárt az idő! 😭").WithColor(Color.Red).Build();
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);
        }

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
                $"+{levels} szint"));
            x.Transactions.Add(
                new Transaction(id, TransactionType.ShopPurchase, -11250000, $"{role.Mention} rang"));
            x.Transactions.Add(new Transaction(id, TransactionType.ShopPurchase, -18750000,
                $"{category.Name} kategória"));
            x.Transactions.Add(new Transaction(id, TransactionType.ShopPurchase, -37500000,
                $"{text.Mention} szövegcsatorna"));
            x.Transactions.Add(new Transaction(id, TransactionType.ShopPurchase, -37500000,
                $"{voice.Mention} hangcsatorna"));
            x.Roles.Add(role.Id);
            x.BoughtChannels.Add(new DiscordChannel(category.Id, DiscordChannelType.Category));
            x.BoughtChannels.Add(new DiscordChannel(text.Id, DiscordChannelType.Text));
            x.BoughtChannels.Add(new DiscordChannel(voice.Id, DiscordChannelType.Voice));
        }).ConfigureAwait(false);
        await UpdateUserAsync(BotUser, x => x.Money += required).ConfigureAwait(false);
        
        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = eb.WithDescription("Sikeres vásárlás 😎!").WithColor(Color.Green).Build();
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
    }

    private static bool VerifyHexColorString(string hexcolor, out uint color)
    {
        if (hexcolor.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase) ||
            hexcolor.StartsWith("&H", StringComparison.CurrentCultureIgnoreCase))
        {
            hexcolor = hexcolor[2..];
        }

        if (hexcolor.StartsWith("#", StringComparison.CurrentCultureIgnoreCase))
        {
            hexcolor = hexcolor[1..];
        }

        var parsedSuccessfully = uint.TryParse(hexcolor, NumberStyles.HexNumber, CultureInfo.CurrentCulture,
            out color);
        return parsedSuccessfully;
    }
}