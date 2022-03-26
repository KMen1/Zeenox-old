using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Models;
using ModalBuilder = Discord.ModalBuilder;

namespace KBot.Modules.Gambling;

public class GamblingComponents : KBotModuleBase
{
    [ComponentInteraction("gamble-shop:*")]
    public async Task HandleShopAsync(string userId, string[] selections)
    {
        await DeferAsync().ConfigureAwait(false);
        if (Context.User.Id != Convert.ToUInt64(userId))
            return;
        var user = Context.Guild.GetUser(Convert.ToUInt64(userId));
        var dbUser = await Database.GetUserAsync(Context.Guild, user).ConfigureAwait(false);
        var msg = ((SocketMessageComponent) Context.Interaction).Message as IUserMessage;
        var eb = msg.Embeds.First().ToEmbedBuilder();
        var item = Enum.Parse<ShopItem>(selections[0]);
        item = selections.Skip(1).Aggregate(item, (current, s) => current | Enum.Parse<ShopItem>(s));

        var choice = "";
        var requiredMoney = 0;
        if ((item & ShopItem.PlusOneLevel) == ShopItem.PlusOneLevel)
        {
            requiredMoney += dbUser.MoneyToBuyLevel(1);
            choice += "`+1 Szint`\n";
        }
        if ((item & ShopItem.PlusTenLevel) == ShopItem.PlusTenLevel)
        {
            requiredMoney += dbUser.MoneyToBuyLevel(10);
            choice += "`+10 Szint`\n";
        }
        if ((item & ShopItem.OwnRank) == ShopItem.OwnRank)
        {
            requiredMoney += 1000000;
            choice += "`Saját rang`\n";
        }
        if ((item & ShopItem.OwnCategory) == ShopItem.OwnCategory)
        {
            requiredMoney += 1000000;
            choice += "`Saját kategória`\n";
        }
        if ((item & ShopItem.OwnTextChannel) == ShopItem.OwnTextChannel)
        {
            requiredMoney += 1000000;
            choice += "`Saját szöveges csatorna`\n";
        }
        if ((item & ShopItem.OwnVoiceChannel) == ShopItem.OwnVoiceChannel)
        {
            requiredMoney += 1000000;
            choice += "`Saját hangcsatorna`\n";
        }
        if (requiredMoney > dbUser.Gambling.Balance)
        {
            var dmsg = ((SocketMessageComponent) Context.Interaction).Message as IUserMessage;
            await FollowupAsync($"Nincs elég KCoin-od a vásárláshoz! (Kell még: {requiredMoney - dbUser.Gambling.Balance} KCoin)")
                .ConfigureAwait(false);
            await dmsg.DeleteAsync().ConfigureAwait(false);
            return;
        }
        eb.AddField("Kiválasztva", choice);
        eb.AddField("Összeg", $"`{requiredMoney.ToString("N0", CultureInfo.CurrentCulture)} KCoin`");
        eb.AddField("Egyenleg", $"**{dbUser.Gambling.Balance}** KCoin -> **{dbUser.Gambling.Balance - requiredMoney}**");
        eb.WithDescription("Biztosan megveszed a kiválasztottakat?");
        
        var comp = new ComponentBuilder()
            .WithButton("Vásárlás", $"gamble-shop-accept:{user.Id}:{(int)item}:{requiredMoney}", ButtonStyle.Success)
            .WithButton("Mégse", "gamble-shop-cancel", ButtonStyle.Danger);
        
        await msg.ModifyAsync(x =>
        {
            x.Embed = eb.Build();
            x.Components = comp.Build();
        }).ConfigureAwait(false);
    }

    [ComponentInteraction("gamble-shop-accept:*:*:*")]
    public async Task HandleShopAcceptAsync(string userId, string type, string requiredMoney)
    {
        if (Context.User.Id != Convert.ToUInt64(userId)) return;
        
        var req = int.Parse(requiredMoney);
        var selection = (ShopItem)int.Parse(type);
        
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        
        var plusOneLevel = (selection & ShopItem.PlusOneLevel) == ShopItem.PlusOneLevel;
        var plusTenLevel = (selection & ShopItem.PlusTenLevel) == ShopItem.PlusTenLevel;
        var ownRank = (selection & ShopItem.OwnRank) == ShopItem.OwnRank;
        var ownCategory = (selection & ShopItem.OwnCategory) == ShopItem.OwnCategory;
        var ownTextChannel = (selection & ShopItem.OwnTextChannel) == ShopItem.OwnTextChannel;
        var ownVoiceChannel = (selection & ShopItem.OwnVoiceChannel) == ShopItem.OwnVoiceChannel;

        if (plusOneLevel || plusTenLevel)
        {
            await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
            {
                if (plusOneLevel)
                {
                    var moneyToBuyOneLevel = dbUser.MoneyToBuyLevel(1);
                    req -= moneyToBuyOneLevel;
                    x.Level++;
                    x.Money -= moneyToBuyOneLevel;
                    x.Transactions.Add(new Transaction("-", TransactionType.ShopPurchase, -moneyToBuyOneLevel, "+1 Szint"));
                }

                if (!plusTenLevel) return;
                var moneyToBuyTenLevel = dbUser.MoneyToBuyLevel(10);
                req -= moneyToBuyTenLevel;
                x.Level += 10;
                x.Money -= moneyToBuyTenLevel;
                x.Transactions.Add(new Transaction("-", TransactionType.ShopPurchase, -moneyToBuyTenLevel, "+10 Szint"));

            }).ConfigureAwait(false);
        }

        if (!ownRank && !ownCategory && !ownTextChannel && !ownVoiceChannel)
        {
            await RespondAsync("Sikeres vásárlás!", ephemeral: true).ConfigureAwait(false);
            return;
        }

        var modal = new ModalBuilder().WithTitle("Vásárlás").WithCustomId($"gamble-shop-accept-modal:{type}:{req}");

        if (ownRank)
        {
            modal.AddTextInput("Rang neve", "rank-name", TextInputStyle.Short, maxLength: 10,
                placeholder: "pl. Disznólovas");
            modal.AddTextInput("Rang színe", "rank-color", TextInputStyle.Short, maxLength: 7,
                placeholder: "HEX színkód");
        }
        if (ownCategory)
        {
            modal.AddTextInput("Kategória neve", "category-name", TextInputStyle.Short,"pl. Kubu főhadiszállás");
        }
        if (ownTextChannel)
        {
            modal.AddTextInput("Szöveges csatorna neve", "text-name", TextInputStyle.Short, "pl. kubu-chat");
        }
        if (ownVoiceChannel)
        {
            modal.AddTextInput("Hangcsatorna neve", "voice-name", TextInputStyle.Short, "pl. Kubu Rezidencia");
        }
        await RespondWithModalAsync(modal.Build()).ConfigureAwait(false);
        await ((SocketMessageComponent) Context.Interaction).Message.DeleteAsync().ConfigureAwait(false);
    }

    [ModalInteraction("gamble-shop-accept-modal:*:*")]
    public async Task HandleShopModalAcceptAsync(string type, int requiredMoney, ShopModal modal)
    {
        await DeferAsync().ConfigureAwait(false);
        var selection = (ShopItem)int.Parse(type);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        
        var ownRank = (selection & ShopItem.OwnRank) == ShopItem.OwnRank;
        var ownCategory = (selection & ShopItem.OwnCategory) == ShopItem.OwnCategory;
        ulong ownCategoryId = 0;
        if (dbUser.BoughtChannels.Exists(x => x.Type == DiscordChannelType.Category))
        {
            ownCategoryId = dbUser.BoughtChannels.First(x => x.Type == DiscordChannelType.Category).Id;
            ownCategory = true;
        }
        var ownTextChannel = (selection & ShopItem.OwnTextChannel) == ShopItem.OwnTextChannel;
        var ownVoiceChannel = (selection & ShopItem.OwnVoiceChannel) == ShopItem.OwnVoiceChannel;
        
        var transactions = new List<Transaction>();
        var channels = new List<DiscordChannel>();
        
        if (ownRank)
        {
            var rankName = modal.RankName;
            var rankColor = uint.Parse(modal.RankColor);
            var role = await Context.Guild.CreateRoleAsync(rankName, GuildPermissions.None, new Color(rankColor)).ConfigureAwait(false);
            await ((SocketGuildUser) Context.User).AddRoleAsync(role).ConfigureAwait(false);
            transactions.Add(new Transaction("-", TransactionType.ShopPurchase, -1000000, $"{role.Mention} rang"));
        }
        if (ownCategory)
        {
            var categoryName = modal.CategoryName;
            ownCategoryId = (await Context.Guild.CreateCategoryChannelAsync(categoryName, x => x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(new []
            {
                new Overwrite(Context.Guild.EveryoneRole.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny)),
                new Overwrite(Context.User.Id, PermissionTarget.User, new OverwritePermissions(manageRoles: PermValue.Allow)),
            })).ConfigureAwait(false)).Id;
            ownCategory = true;
            transactions.Add(new Transaction("-", TransactionType.ShopPurchase, -1000000, $"{categoryName} kategória"));
            channels.Add(new DiscordChannel(ownCategoryId, DiscordChannelType.Category));
        }
        if (ownTextChannel)
        {
            var textName = modal.TextName;
            var channel = await Context.Guild.CreateTextChannelAsync(textName, x =>
            {
                x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(new[]
                {
                    new Overwrite(Context.Guild.EveryoneRole.Id, PermissionTarget.Role,
                        new OverwritePermissions(viewChannel: PermValue.Deny)),
                    new Overwrite(Context.User.Id, PermissionTarget.User,
                        new OverwritePermissions(manageRoles: PermValue.Allow)),
                });
                x.CategoryId = ownCategory ? ownCategoryId : null;
            }).ConfigureAwait(false);
            transactions.Add(new Transaction("-", TransactionType.ShopPurchase, -1000000, $"{channel.Mention} csatorna"));
            channels.Add(new DiscordChannel(channel.Id, DiscordChannelType.Text));
        }
        if (ownVoiceChannel)
        {
            var voiceName = modal.VoiceName;
            await Context.Guild.CreateVoiceChannelAsync(voiceName, x =>
            {
                x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(new[]
                {
                    new Overwrite(Context.Guild.EveryoneRole.Id, PermissionTarget.Role,
                        new OverwritePermissions(viewChannel: PermValue.Deny)),
                    new Overwrite(Context.User.Id, PermissionTarget.User,
                        new OverwritePermissions(manageRoles: PermValue.Allow)),
                });
                x.CategoryId = ownCategory ? ownCategoryId : null;
            }).ConfigureAwait(false);
            transactions.Add(new Transaction("-", TransactionType.ShopPurchase, -1000000, $"{voiceName} hangcsatorna"));
            channels.Add(new DiscordChannel(ownCategoryId, DiscordChannelType.Voice));
        }

        await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
        {
            x.Money -= requiredMoney;
            x.Transactions.AddRange(transactions);
            x.BoughtChannels.AddRange(channels);
        }).ConfigureAwait(false);
        
        await FollowupAsync("Sikeres vásárlás!", ephemeral: true).ConfigureAwait(false);
    }
    
    [ComponentInteraction("gamble-shop-cancel")]
    public async Task HandleShopCancelAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var msg = ((SocketMessageComponent) Context.Interaction).Message as IUserMessage;
        await msg.DeleteAsync().ConfigureAwait(false);
    }

    public class ShopModal : IModal
    {
        public string Title => "Vásárlás";

        [ModalTextInput("rank-name")]
        public string RankName { get; set; }

        [ModalTextInput("rank-color")]
        public string RankColor { get; set; }

        [ModalTextInput("category-name")]
        public string CategoryName { get; set; }

        [ModalTextInput("text-name")]
        public string TextName { get; set; }

        [ModalTextInput("voice-name")]
        public string VoiceName { get; set; }
    }
}
