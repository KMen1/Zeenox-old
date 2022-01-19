using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Database;
using KBot.Enums;

namespace KBot.Modules.Fun;

public class Levels : KBotModuleBase
{
    public DatabaseService Database { get; set; }
    
    [RequireOwner]
    [SlashCommand("registerguild", "Regisztrálja a szerverre a botot")]
    public async Task RegisterGuild()
    {
        await DeferAsync();
        await Database.RegisterGuild(Context.Guild.Id);
        await FollowupAsync("Kész");
    }
    
    [SlashCommand("level", "Saját/más szint és xp lekérése")]
    public async Task GetLevel(SocketUser user = null)
    {
        await DeferAsync();
        var setUser = user ?? Context.User;
        var userId = setUser.Id;
        
        var level = await Database.GetUserLevelById(Context.Guild.Id, userId);
        var xp = await Database.GetUserPointsById(Context.Guild.Id, userId);

        var embed = new EmbedBuilder()
            .WithAuthor(setUser.Username, setUser.GetAvatarUrl())
            .WithColor(Color.Gold)
            .WithDescription($"**XP: **`{xp}/18000` ({level * 18000 + xp} Összesen) \n**Szint: **`{level}`")
            .Build();

        await FollowupAsync(embed: embed);
    }

    [SlashCommand("daily", "Napi XP begyűjtése")]
    public async Task GetDaily()
    {
        await DeferAsync();
        var userId = Context.User.Id;
        var lastDaily = await Database.GetDailyClaimDateById(Context.Guild.Id, userId);
        var canClaim = lastDaily.AddDays(1) < DateTime.Now;
        if (lastDaily == DateTime.MinValue || canClaim)
        {
            var xp = new Random().Next(100, 500);
            await Database.SetDailyClaimDateById(Context.Guild.Id, userId, DateTime.Now);
            await Database.AddPointsByUserId(Context.Guild.Id, userId, xp);
            await FollowupWithEmbedAsync(EmbedResult.Success, "Sikeresen begyűjtetted a napi XP-d!", $"A begyűjtött XP mennyisége: {xp}");
        }
        else
        {
            var timeLeft = lastDaily.AddDays(1) - DateTime.Now;
            await FollowupWithEmbedAsync(EmbedResult.Error, "Sikertelen begyűjtés", $"Gyere vissza {timeLeft.Days} nap, {timeLeft.Hours} óra, {timeLeft.Minutes} perc és {timeLeft.Seconds} másodperc múlva!");
        }
    }
    [SlashCommand("setpoints", "XP állítása (admin)")]
    public async Task SetPoints(SocketUser user, int points)
    {
        await DeferAsync();
        var newPoints = await Database.SetPointsByUserId(Context.Guild.Id, user.Id, points);
        await FollowupWithEmbedAsync(EmbedResult.Success, "Pontok hozzáadva!", $"{user.Mention} mostantól {newPoints} XP-vel rendelkezik!");
    }
    [SlashCommand("addpoints", "XP hozzáadása (admin)")]
    public async Task AddPoints(SocketUser user, int pointsToAdd)
    {
        await DeferAsync();
        var newPoints = await Database.AddPointsByUserId(Context.Guild.Id, user.Id, pointsToAdd);
        await FollowupWithEmbedAsync(EmbedResult.Success, "Pontok beállítva!", $"{user.Mention} mostantól {newPoints} XP-vel rendelkezik!");
    }
    
    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("addlevel", "Szint hozzáadása (admin)")]
    public async Task AddLevel(SocketUser user, int levelsToAdd)
    {
        await DeferAsync();
        var level = await Database.AddLevelByUserId(Context.Guild.Id, user.Id, levelsToAdd);
        await FollowupWithEmbedAsync(EmbedResult.Success, "Szint hozzáadva!", $"{user.Mention} mostantól {level} szintű!");
    }
    
    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("setlevel", "Szint állítása (admin)")]
    public async Task SetLevel(SocketUser user, int level)
    {
        await DeferAsync();
        var newLevel = await Database.SetLevelByUserId(Context.Guild.Id, user.Id, level);
        await FollowupWithEmbedAsync(EmbedResult.Success, "Szint hozzáadva!", $"{user.Mention} mostantól {newLevel} szintű!");
    }
}