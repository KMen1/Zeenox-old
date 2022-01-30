using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Database;
using KBot.Enums;

namespace KBot.Modules.Setup;

public class SetupCommands : KBotModuleBase
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("setup", "Bot beállítása")]
    public async Task SetupGuildAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var setupEmbed = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = Context.Guild.Name,
                IconUrl = Context.Guild.IconUrl
            },
            Title = "KBot Beállítás",
            Description = "Kérlek erősítsd meg, hogy el akarod indítani a beállítása folyamatot",
            Color = Color.Gold,
            Timestamp = DateTimeOffset.Now
        };
        var confirmationComponents = new ComponentBuilder()
            .WithButton("Folytatás", "setup-yes", ButtonStyle.Success, new Emoji("✅"))
            .WithButton("Mégse", "setup-cancel", ButtonStyle.Danger, new Emoji("❌"))
            .Build();

        var setupMessage =
            await FollowupAsync(embed: setupEmbed.Build(), components: confirmationComponents).ConfigureAwait(false);

        var confirmationMsgResult = await InteractiveService
            .NextMessageComponentAsync(x => x.Message.Id == setupMessage.Id && x.User.Id == Context.User.Id).ConfigureAwait(false);

        if (!confirmationMsgResult.IsSuccess)
        {
            return;
        }

        var config = new GuildConfig
        {
            Announcements = new AnnouncementConfig
            {
                UserBanAnnouncementChannelId = 0,
                UserUnbanAnnouncementChannelId = 0,
                UserJoinAnnouncementChannelId = 0,
                UserLeaveAnnouncementChannelId = 0,
            },
            Leveling = new LevelingConfig
            {
                LevelUpAnnouncementChannelId = 0,
                PointsToLevelUp = 0
            },
            MovieEvents = new MovieConfig
            {
                EventAnnouncementChannelId = 0,
                RoleId = 0,
                StreamingChannelId = 0
            },
            TemporaryChannels = new TemporaryVoiceChannelConfig
            {
                CategoryId = 0,
                CreateChannelId = 0
            },
            TourEvents = new TourConfig
            {
                EventAnnouncementChannelId = 0,
                RoleId = 0
            }
        };

        await setupMessage.ModifyAsync(x =>
        {
            x.Embed = setupEmbed
                .WithDescription(
                    "Add meg annak a csatornának az id-jét ahol a bot **üdvözölje** az új felhasználókat. \n" +
                    "`Ha nem szeretnéd beállítani, írj egy 0-át.`")
                .Build();
            x.Components = null;
        }).ConfigureAwait(false);

        var joinChannelIdMessage = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
        var joinChannelId = Convert.ToUInt64(joinChannelIdMessage.Value!.Content);
        while (Context.Guild.GetTextChannel(joinChannelId) is null)
        {
            await setupMessage.ModifyAsync(x => x.Embed = setupEmbed
                    .WithColor(Color.Red)
                    .WithDescription("Nem található ilyen csatorna! Kérlek próbáld újra")
                    .Build()).ConfigureAwait(false);
            joinChannelIdMessage = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
            joinChannelId = Convert.ToUInt64(joinChannelIdMessage.Value!.Content);
        }
        config.Announcements.UserJoinAnnouncementChannelId = joinChannelId;
        await joinChannelIdMessage.Value.DeleteAsync().ConfigureAwait(false);

        await setupMessage.ModifyAsync(x =>
        {
            x.Embed = setupEmbed
                .WithDescription(
                    "Add meg annak a csatornának az id-jét ahol a bot **elköszöntse** a felhasználókat. \n" +
                    "`Ha nem szeretnéd beállítani, írj egy 0-t.`")
                .Build();
            x.Components = null;
        }).ConfigureAwait(false);

        var leaveChannelIdMessage = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
        var leaveChannelId = Convert.ToUInt64(leaveChannelIdMessage.Value!.Content);
        while (Context.Guild.GetTextChannel(leaveChannelId) is null)
        {
            await setupMessage.ModifyAsync(x => x.Embed = setupEmbed
                    .WithColor(Color.Red)
                    .WithDescription("Nem található ilyen csatorna! Kérlek próbáld újra")
                    .Build()).ConfigureAwait(false);
            leaveChannelIdMessage = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
            leaveChannelId = Convert.ToUInt64(leaveChannelIdMessage.Value!.Content);
        }
        config.Announcements.UserLeaveAnnouncementChannelId = leaveChannelId;
        await leaveChannelIdMessage.Value.DeleteAsync().ConfigureAwait(false);
        
        await setupMessage.ModifyAsync(x =>
        {
            x.Embed = setupEmbed
                .WithDescription(
                    "Add meg annak a csatornának az id-jét ahol a bot bejelentse a **bannokat**. \n" +
                    "`Ha nem szeretnéd beállítani, írj egy 0-t.`")
                .Build();
            x.Components = null;
        }).ConfigureAwait(false);
        
        var banChannelIdMessage = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
        var banChannelId = Convert.ToUInt64(banChannelIdMessage.Value!.Content);
        while (Context.Guild.GetTextChannel(banChannelId) is null)
        {
            await setupMessage.ModifyAsync(x => x.Embed = setupEmbed
                    .WithColor(Color.Red)
                    .WithDescription("Nem található ilyen csatorna! Kérlek próbáld újra")
                    .Build()).ConfigureAwait(false);
            banChannelIdMessage = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
            banChannelId = Convert.ToUInt64(banChannelIdMessage.Value!.Content);
        }
        config.Announcements.UserBanAnnouncementChannelId = banChannelId;
        await banChannelIdMessage.Value.DeleteAsync().ConfigureAwait(false);

        await setupMessage.ModifyAsync(x =>
        {
            x.Embed = setupEmbed
                .WithDescription(
                    "Add meg annak a csatornának az id-jét ahol a bot bejelentse az **unbannokat**. \n" +
                    "`Ha nem szeretnéd beállítani, írj egy 0-t.`")
                .Build();
            x.Components = null;
        }).ConfigureAwait(false);
        
        var unBanChannelIdMessage = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
        var unBanChannelId = Convert.ToUInt64(unBanChannelIdMessage.Value!.Content);
        while (Context.Guild.GetTextChannel(unBanChannelId) is null)
        {
            await setupMessage.ModifyAsync(x => x.Embed = setupEmbed
                    .WithColor(Color.Red)
                    .WithDescription("Nem található ilyen csatorna! Kérlek próbáld újra")
                    .Build()).ConfigureAwait(false);
            unBanChannelIdMessage = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
            unBanChannelId = Convert.ToUInt64(unBanChannelIdMessage.Value!.Content);
        }
        config.Announcements.UserUnbanAnnouncementChannelId = unBanChannelId;
        await unBanChannelIdMessage.Value.DeleteAsync().ConfigureAwait(false);

        await setupMessage.ModifyAsync(x =>
        {
            x.Embed = setupEmbed
                .WithDescription(
                    "Add meg annak a csatornának az id-jét ahol a bot bejelentse a **szintlépéseket**. \n" +
                    "`Ha nem szeretnéd beállítani, írj egy 0-t.`")
                .Build();
            x.Components = null;
        }).ConfigureAwait(false);

        var levelChannelIdMessage = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
        var levelChannelId = Convert.ToUInt64(levelChannelIdMessage.Value!.Content);
        while (Context.Guild.GetTextChannel(unBanChannelId) is null)
        {
            await setupMessage.ModifyAsync(x => x.Embed = setupEmbed
                    .WithColor(Color.Red)
                    .WithDescription("Nem található ilyen csatorna! Kérlek próbáld újra")
                    .Build()).ConfigureAwait(false);
            levelChannelIdMessage = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
            levelChannelId = Convert.ToUInt64(levelChannelIdMessage.Value!.Content);
        }
        config.Leveling.LevelUpAnnouncementChannelId = levelChannelId;
        await levelChannelIdMessage.Value.DeleteAsync().ConfigureAwait(false);

        await setupMessage.ModifyAsync(x =>
        {
            x.Embed = setupEmbed
                .WithDescription(
                    "Add meg hány XP-be teljen egy szintet elérni. \n" +
                    "`Ha nem szeretnéd beállítani, írj egy 0-t.`")
                .Build();
            x.Components = null;
        }).ConfigureAwait(false);

        var pointsMessage = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
        var points = Convert.ToInt32(pointsMessage.Value!.Content);
        config.Leveling.PointsToLevelUp = points;
        await pointsMessage.Value.DeleteAsync().ConfigureAwait(false);

        await setupMessage.ModifyAsync(x =>
        {
            x.Embed = setupEmbed
                .WithDescription(
                    "Kérlek add meg az ideiglenes hangcsatornák kategóriájának id-jét. \n" +
                    "`Ha nem szeretnéd beállítani, írj egy 0-t.`")
                .Build();
            x.Components = null;
        }).ConfigureAwait(false);
        
        var temporaryCategoryIdMessage = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
        var temporaryCategoryId = Convert.ToUInt64(temporaryCategoryIdMessage.Value!.Content);
        while (Context.Guild.GetCategoryChannel(temporaryCategoryId) is null)
        {
            await setupMessage.ModifyAsync(x => x.Embed = setupEmbed
                .WithColor(Color.Red)
                .WithDescription("Nem található ilyen csatorna! Kérlek próbáld újra")
                .Build()).ConfigureAwait(false);
            temporaryCategoryIdMessage = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
            temporaryCategoryId = Convert.ToUInt64(temporaryCategoryIdMessage.Value!.Content);
        }
        config.TemporaryChannels.CategoryId = temporaryCategoryId;
        await temporaryCategoryIdMessage.Value.DeleteAsync().ConfigureAwait(false);
        
        await setupMessage.ModifyAsync(x =>
        {
            x.Embed = setupEmbed
                .WithDescription(
                    "Kérlek add meg az ideiglenes hangcsatornák létrehozás csatornájának az id-jét. \n" +
                    "`Ha nem szeretnéd beállítani, írj egy 0-t.`")
                .Build();
            x.Components = null;
        }).ConfigureAwait(false);
        
        var temporaryCreateIdMessage = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
        var temporaryCreateId = Convert.ToUInt64(temporaryCreateIdMessage.Value!.Content);
        while (Context.Guild.GetVoiceChannel(temporaryCreateId) is null)
        {
            await setupMessage.ModifyAsync(x => x.Embed = setupEmbed
                .WithColor(Color.Red)
                .WithDescription("Nem található ilyen csatorna! Kérlek próbáld újra")
                .Build()).ConfigureAwait(false);
            temporaryCreateIdMessage = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
            temporaryCreateId = Convert.ToUInt64(temporaryCreateIdMessage.Value!.Content);
        }
        config.TemporaryChannels.CreateChannelId = temporaryCreateId;
        await temporaryCreateIdMessage.Value.DeleteAsync().ConfigureAwait(false);
        await setupMessage.ModifyAsync(x =>
        {
            x.Embed = setupEmbed
                .WithDescription(
                    "Kérlek add meg a film események bejelentő csatornájának az id-jét. \n" +
                    "`Ha nem szeretnéd beállítani, írj egy 0-t.`")
                .Build();
            x.Components = null;
        }).ConfigureAwait(false);
        
        var movieAnnouncement = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
        var movieAnnouncementId = Convert.ToUInt64(movieAnnouncement.Value!.Content);
        while (Context.Guild.GetTextChannel(movieAnnouncementId) is null)
        {
            await setupMessage.ModifyAsync(x => x.Embed = setupEmbed
                .WithColor(Color.Red)
                .WithDescription("Nem található ilyen csatorna! Kérlek próbáld újra")
                .Build()).ConfigureAwait(false);
            movieAnnouncement = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
            movieAnnouncementId = Convert.ToUInt64(movieAnnouncement.Value!.Content);
        }
        config.MovieEvents.EventAnnouncementChannelId = movieAnnouncementId;
        await movieAnnouncement.Value.DeleteAsync().ConfigureAwait(false);

        await setupMessage.ModifyAsync(x =>
        {
            x.Embed = setupEmbed
                .WithDescription(
                    "Kérlek add meg a film események vetítő csatornájának az id-jét. \n" +
                    "`Ha nem szeretnéd beállítani, írj egy 0-t.`")
                .Build();
            x.Components = null;
        }).ConfigureAwait(false);
        
        var movieStreaming = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
        var movieStreamingId = Convert.ToUInt64(movieStreaming.Value!.Content);
        while (Context.Guild.GetVoiceChannel(movieStreamingId) is null)
        {
            await setupMessage.ModifyAsync(x => x.Embed = setupEmbed
                .WithColor(Color.Red)
                .WithDescription("Nem található ilyen csatorna! Kérlek próbáld újra")
                .Build()).ConfigureAwait(false);
            movieStreaming = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
            movieStreamingId = Convert.ToUInt64(movieStreaming.Value!.Content);
        }
        config.MovieEvents.StreamingChannelId = movieStreamingId;
        await movieStreaming.Value.DeleteAsync().ConfigureAwait(false);
        
        await setupMessage.ModifyAsync(x =>
        {
            x.Embed = setupEmbed
                .WithDescription(
                    "Kérlek add meg a film rang id-jét. \n" +
                    "`Ha nem szeretnéd beállítani, írj egy 0-t.`")
                .Build();
            x.Components = null;
        }).ConfigureAwait(false);
        
        var movieRank = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
        var movieRankId = Convert.ToUInt64(movieRank.Value!.Content);
        while (Context.Guild.GetRole(movieRankId) is null)
        {
            await setupMessage.ModifyAsync(x => x.Embed = setupEmbed
                .WithColor(Color.Red)
                .WithDescription("Nem található ilyen rang! Kérlek próbáld újra")
                .Build()).ConfigureAwait(false);
            movieRank = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
            movieRankId = Convert.ToUInt64(movieRank.Value!.Content);
        }
        config.MovieEvents.RoleId = movieRankId;
        await movieRank.Value.DeleteAsync().ConfigureAwait(false);
        
        await setupMessage.ModifyAsync(x =>
        {
            x.Embed = setupEmbed
                .WithDescription(
                    "Kérlek add meg a túra események bejelentő csatornájának az id-jét. \n" +
                    "`Ha nem szeretnéd beállítani, írj egy 0-t.`")
                .Build();
            x.Components = null;
        }).ConfigureAwait(false);
        
        var tourChannel = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
        var tourChannelId = Convert.ToUInt64(tourChannel.Value!.Content);
        while (Context.Guild.GetTextChannel(tourChannelId) is null)
        {
            await setupMessage.ModifyAsync(x => x.Embed = setupEmbed
                .WithColor(Color.Red)
                .WithDescription("Nem található ilyen csatorna! Kérlek próbáld újra")
                .Build()).ConfigureAwait(false);
            tourChannel = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
            tourChannelId = Convert.ToUInt64(tourChannel.Value!.Content);
        }
        config.TourEvents.EventAnnouncementChannelId = tourChannelId;
        await tourChannel.Value.DeleteAsync().ConfigureAwait(false);
        
        await setupMessage.ModifyAsync(x =>
        {
            x.Embed = setupEmbed
                .WithDescription(
                    "Kérlek add meg a túra rang id-jét. \n" +
                    "`Ha nem szeretnéd beállítani, írj egy 0-t.`")
                .Build();
            x.Components = null;
        }).ConfigureAwait(false);
        
        var tourRank = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
        var tourRankId = Convert.ToUInt64(tourRank.Value!.Content);
        while (Context.Guild.GetRole(tourRankId) is null)
        {
            await setupMessage.ModifyAsync(x => x.Embed = setupEmbed
                .WithColor(Color.Red)
                .WithDescription("Nem található ilyen csatorna! Kérlek próbáld újra")
                .Build()).ConfigureAwait(false);
            tourRank = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id).ConfigureAwait(false);
            tourRankId = Convert.ToUInt64(tourRank.Value!.Content);
        }
        config.TourEvents.RoleId = tourRankId;
        await tourRank.Value!.DeleteAsync().ConfigureAwait(false);

        
        var finalEmbed = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = Context.Guild.Name,
                IconUrl = Context.Guild.IconUrl
            },
            Title = "KBot Beállítás",
            Description = "",
            Color = Color.Green,
            Fields =
            {
                new()
                {
                    Name = "Bejelentések",
                    Value = $"- Üdvözlő csatorna: {Context.Guild.GetTextChannel(config.Announcements.UserJoinAnnouncementChannelId).Mention} \n" +
                            $"- Kilépő csatorna: {Context.Guild.GetTextChannel(config.Announcements.UserLeaveAnnouncementChannelId).Mention} \n" +
                            $"- Ban csatorna: {Context.Guild.GetTextChannel(config.Announcements.UserBanAnnouncementChannelId).Mention} \n" +
                            $"- Unban csatorna: {Context.Guild.GetTextChannel(config.Announcements.UserUnbanAnnouncementChannelId).Mention}"
                },
                new()
                {
                    Name = "Szintrendszer",
                    Value = $"- Szintlépések csatorna: {Context.Guild.GetTextChannel(config.Leveling.LevelUpAnnouncementChannelId).Mention} \n" +
                            $"- Szintlépéshez szükséges pontok száma: `{config.Leveling.PointsToLevelUp}`"
                },
                new()
                {
                    Name = "Ideiglenes csatornák",
                    Value = $"- Kategória: `{Context.Guild.GetCategoryChannel(config.TemporaryChannels.CategoryId)}` \n" +
                            $"- Létrehozás csatorna: {Context.Guild.GetVoiceChannel(config.TemporaryChannels.CreateChannelId).Mention}"
                },
                new()
                {
                    Name = "Film események",
                    Value = $"- Bejelentő csatorna: {Context.Guild.GetTextChannel(config.MovieEvents.EventAnnouncementChannelId).Mention} \n" +
                            $"- Vetítő csatorna: {Context.Guild.GetVoiceChannel(config.MovieEvents.StreamingChannelId).Mention} \n" +
                            $"- Rang: {Context.Guild.GetRole(config.MovieEvents.RoleId).Mention}"
                },
                new()
                {
                    Name = "Túra események",
                    Value = $"- Bejelentő csatorna: {Context.Guild.GetTextChannel(config.TourEvents.EventAnnouncementChannelId).Mention} \n" +
                            $"- Rang: {Context.Guild.GetRole(config.TourEvents.RoleId).Mention}"
                }
            }
        }.Build();

        var finalComponents = new ComponentBuilder()
            .WithButton("Mentés", "setup-save", ButtonStyle.Success, new Emoji("✅"))
            .WithButton("Mégse", "setup-cancel", ButtonStyle.Danger, new Emoji("❌"))
            .Build();

        await setupMessage.ModifyAsync(x =>
        {
            x.Embed = finalEmbed;
            x.Components = finalComponents;
        }).ConfigureAwait(false);

        var finalResult = await InteractiveService.NextMessageComponentAsync(x => x.Message.Id == setupMessage.Id && x.User.Id == Context.User.Id).ConfigureAwait(false);
        if (!finalResult.IsSuccess)
        {
            await finalResult.Value!.DeferAsync().ConfigureAwait(false);
            return;
        }
        
        await Database.SaveGuildConfigAsync(Context.Guild.Id, config).ConfigureAwait(false);
        await setupMessage.ModifyAsync(x =>
        {
            x.Embed = new EmbedBuilder().Build();
            x.Components = new ComponentBuilder().Build();
            x.Content = "Beállítások sikeresen elmentve!";
        }).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("config", "Bot beállításai")]
    public async Task SendGuildConfigAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var config = await Database.GetGuildConfigAsync(Context.Guild.Id).ConfigureAwait(false);
        if (config == null)
        {
            await FollowupWithEmbedAsync(EmbedResult.Error, "Sikertelen lekérés", "Ez a szerver nem lett beállítva! Kérlek használd a `setup` parancsot!").ConfigureAwait(false);
            return;
        }
        
        var configEmbed = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = Context.Guild.Name,
                IconUrl = Context.Guild.IconUrl
            },
            Title = "KBot Beállítás",
            Description = "",
            Color = Color.Green,
            Fields =
            {
                new()
                {
                    Name = "Bejelentések",
                    Value = $"- Üdvözlő csatorna: {Context.Guild.GetTextChannel(config.Announcements.UserJoinAnnouncementChannelId).Mention} \n" +
                            $"- Kilépő csatorna: {Context.Guild.GetTextChannel(config.Announcements.UserLeaveAnnouncementChannelId).Mention} \n" +
                            $"- Ban csatorna: {Context.Guild.GetTextChannel(config.Announcements.UserBanAnnouncementChannelId).Mention} \n" +
                            $"- Unban csatorna: {Context.Guild.GetTextChannel(config.Announcements.UserUnbanAnnouncementChannelId).Mention}"
                },
                new()
                {
                    Name = "Szintrendszer",
                    Value = $"- Szintlépések csatorna: {Context.Guild.GetTextChannel(config.Leveling.LevelUpAnnouncementChannelId).Mention} \n" +
                            $"- Szintlépéshez szükséges pontok száma: `{config.Leveling.PointsToLevelUp}`"
                },
                new()
                {
                    Name = "Ideiglenes csatornák",
                    Value = $"- Kategória: `{Context.Guild.GetCategoryChannel(config.TemporaryChannels.CategoryId)}` \n" +
                            $"- Létrehozás csatorna: {Context.Guild.GetVoiceChannel(config.TemporaryChannels.CreateChannelId).Mention}"
                },
                new()
                {
                    Name = "Film események",
                    Value = $"- Bejelentő csatorna: {Context.Guild.GetTextChannel(config.MovieEvents.EventAnnouncementChannelId).Mention} \n" +
                            $"- Vetítő csatorna: {Context.Guild.GetVoiceChannel(config.MovieEvents.StreamingChannelId).Mention} \n" +
                            $"- Rang: {Context.Guild.GetRole(config.MovieEvents.RoleId).Mention}"
                },
                new()
                {
                    Name = "Túra események",
                    Value = $"- Bejelentő csatorna: {Context.Guild.GetTextChannel(config.TourEvents.EventAnnouncementChannelId).Mention} \n" +
                            $"- Rang: {Context.Guild.GetRole(config.TourEvents.RoleId).Mention}"
                }
            }
        }.Build();
        await FollowupAsync(embed: configEmbed).ConfigureAwait(false);
    }

    [ComponentInteraction("setup-cancel")]
    public async Task SetupCancelAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await ((SocketMessageComponent)Context.Interaction).Message.DeleteAsync().ConfigureAwait(false);
    }
}