using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discordance.Enums;
using Discordance.Extensions;
using Discordance.Models;
using Discordance.Services;
using Hangfire;
using Game = Discordance.Models.Game;

namespace Discordance.Modules.Notifications;

public class NotificationService
{
    private readonly DiscordShardedClient _client;
    private readonly MongoService _mongo;
    private readonly ImageService _imageService;
    private readonly HttpClient _httpClient;
    private IEnumerable<Game> CachedGames { get; set; } = null!;
    private IEnumerable<Perk> CachedPerks { get; set; } = null!;
    private const string ShrineUrl = "https://dbd.onteh.net.au/api/shrine/";
    private const string Url =
        "https://store-site-backend-static-ipv4.ak.epicgames.com/freeGamesPromotions?locale=en-US&country=US&allowCountries=HU";

    public NotificationService(
        DiscordShardedClient client,
        MongoService mongoService,
        ImageService imageService,
        HttpClient httpClient
    )
    {
        _client = client;
        _mongo = mongoService;
        _imageService = imageService;
        _httpClient = httpClient;
        client.UserJoined += HandleUserJoinedAsync;
        client.UserLeft += HandleUserLeaveAsync;
        client.UserBanned += CallBanFunctions;
        client.UserUnbanned += CallUnbanFunctions;

        /*RecurringJob.AddOrUpdate(
            "epic",
            () => RefreshAndNotifyAsync(NotificationSource.Epic),
            "5 17 * * THU"
        );
        RecurringJob.AddOrUpdate(
            "dbd",
            () => RefreshAndNotifyAsync(NotificationSource.Dbd),
            "0 0 * * WED"
        );*/
    }

    private async Task HandleUserJoinedAsync(SocketGuildUser user)
    {
        if (user.IsBot || user.IsWebhook)
            return;

        var guild = user.Guild;
        var config = await _mongo.GetGuildConfigAsync(guild.Id).ConfigureAwait(false);

        if (config.Notifications.WelcomeInChannel)
        {
            var channel = guild.GetTextChannel(config.Notifications.WelcomeChannelId);
            if (channel is null)
            {
                await user.Guild.Owner
                    .SendMessageAsync(
                        embed: new EmbedBuilder()
                            .WithColor(Color.Red)
                            .WithTitle("Unable to send welcome message")
                            .WithDescription(
                                "The channel set to receive welcome messages could not be found!\n"
                                    + "Welcome messages have been disabled until a new channel is set!"
                            )
                            .Build()
                    )
                    .ConfigureAwait(false);

                await _mongo
                    .UpdateGuildConfig(guild.Id, x => x.Notifications.WelcomeInChannel = false)
                    .ConfigureAwait(false);
                return;
            }

            if (config.Notifications.SendWelcomeImage)
            {
                var image = await _imageService.CreateWelcomeImageAsync(user).ConfigureAwait(false);
                await channel
                    .SendFileAsync(image, $"{Guid.NewGuid()}.png", "")
                    .ConfigureAwait(false);
                return;
            }

            await AnnounceEvent(
                    AnnounceType.Welcome,
                    config.Notifications.WelcomeMessage,
                    channel,
                    user,
                    user.Guild
                )
                .ConfigureAwait(false);
        }

        if (config.AutoRole && config.AutoRoleIds.Count > 0)
        {
            await AddRolesToUser(user, config.AutoRoleIds).ConfigureAwait(false);
        }
    }

    private async Task HandleUserLeaveAsync(SocketGuild guild, SocketUser user)
    {
        if (user.IsBot || user.IsWebhook)
            return;

        var config = await _mongo.GetGuildConfigAsync(guild.Id).ConfigureAwait(false);
        if (!config.Notifications.AnnounceLeave)
            return;

        var channel = guild.GetTextChannel(config.Notifications.GoodbyeChannelId);
        if (channel is null)
        {
            await guild.Owner
                .SendMessageAsync(
                    embed: new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithTitle("Unable to send goodbye message")
                        .WithDescription(
                            "The channel set to receive goodbye messages could not be found!\n"
                                + "Goodbye messages have been disabled until a new channel is set!"
                        )
                        .Build()
                )
                .ConfigureAwait(false);

            await _mongo
                .UpdateGuildConfig(guild.Id, x => x.Notifications.AnnounceLeave = false)
                .ConfigureAwait(false);
            return;
        }

        await AnnounceEvent(
                AnnounceType.Leave,
                config.Notifications.GoodbyeMessage,
                channel,
                user,
                guild
            )
            .ConfigureAwait(false);
    }

    private async Task CallBanFunctions(SocketUser user, SocketGuild guild)
    {
        if (user.IsBot || user.IsWebhook)
            return;

        var config = await _mongo.GetGuildConfigAsync(guild.Id).ConfigureAwait(false);
        if (!config.Notifications.AnnounceBan)
            return;

        var channel = guild.GetTextChannel(config.Notifications.BanChannelId);
        if (channel is null)
        {
            await guild.Owner
                .SendMessageAsync(
                    embed: new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithTitle("Unable to send ban announcement")
                        .WithDescription(
                            "The channel set to receive ban announcements could not be found!\n"
                                + "Ban announcements have been disabled until a new channel is set!"
                        )
                        .Build()
                )
                .ConfigureAwait(false);

            await _mongo
                .UpdateGuildConfig(guild.Id, x => x.Notifications.AnnounceBan = false)
                .ConfigureAwait(false);
            return;
        }

        await AnnounceEvent(AnnounceType.Ban, config.Notifications.BanMessage, channel, user, guild)
            .ConfigureAwait(false);
    }

    private async Task CallUnbanFunctions(SocketUser user, SocketGuild guild)
    {
        if (user.IsBot || user.IsWebhook)
            return;

        var config = await _mongo.GetGuildConfigAsync(guild.Id).ConfigureAwait(false);
        if (!config.Notifications.AnnounceUnban)
            return;

        var channel = guild.GetTextChannel(config.Notifications.UnbanChannelId);
        if (channel is null)
        {
            await guild.Owner
                .SendMessageAsync(
                    embed: new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithTitle("Unable to send unban announcement")
                        .WithDescription(
                            "The channel set to receive unban announcements could not be found!\n"
                                + "Unban announcements have been disabled until a new channel is set!"
                        )
                        .Build()
                )
                .ConfigureAwait(false);

            await _mongo
                .UpdateGuildConfig(guild.Id, x => x.Notifications.AnnounceUnban = false)
                .ConfigureAwait(false);
            return;
        }

        await AnnounceEvent(
                AnnounceType.Unban,
                config.Notifications.UnbanMessage,
                channel,
                user,
                guild
            )
            .ConfigureAwait(false);
    }

    private static async Task AnnounceEvent(
        AnnounceType type,
        string rawMessage,
        IMessageChannel channel,
        IUser user,
        IGuild? guild = null
    )
    {
        var defaultMessage = "";
        switch (type)
        {
            case AnnounceType.Welcome:
            {
                defaultMessage = $"Welcome to **{guild?.Name}** {user.Mention}!";
                break;
            }
            case AnnounceType.Ban:
            {
                defaultMessage = $"{user.Username}#{user.Discriminator} has been banned!";
                break;
            }
            case AnnounceType.Unban:
            {
                defaultMessage = $"{user.Username}#{user.Discriminator} has been unbanned!";
                break;
            }
            case AnnounceType.Leave:
            {
                defaultMessage = $"{user.Username}#{user.Discriminator} left the server!";
                break;
            }
        }

        var message = string.IsNullOrEmpty(rawMessage)
          ? defaultMessage
          : GenericExtensions.ParseMessage(rawMessage, user, guild);

        await channel.SendMessageAsync(message).ConfigureAwait(false);
    }

    private async Task AddRolesToUser(SocketGuildUser user, List<ulong> roleIds)
    {
        var guild = user.Guild;

        var wrongIndexes = new List<ulong>();
        foreach (var roleId in roleIds)
        {
            var role = guild.GetRole(roleId);
            if (role is null)
            {
                wrongIndexes.Add(roleId);
                continue;
            }

            await user.AddRoleAsync(role).ConfigureAwait(false);
        }

        if (wrongIndexes.Count > 0)
        {
            var correctedRoleIds = roleIds.Except(wrongIndexes).ToList();
            await _mongo
                .UpdateGuildConfig(guild.Id, x => x.AutoRoleIds = correctedRoleIds)
                .ConfigureAwait(false);
        }
    }

    public static string GetCharacterNameFromId(long jsonCharacter)
    {
        return jsonCharacter switch
        {
            0 => "Dwight Fairfield",
            1 => "Meg Thomas",
            2 => "Claudette Morel",
            3 => "Jake Park",
            4 => "Nea Karlsson",
            5 => "Laurie Strode",
            6 => "Ace Visconti",
            7 => "William \"Bill\" Overbeck",
            8 => "Feng Min",
            9 => "David King",
            10 => "Kate Denson",
            11 => "Quentin Smith",
            12 => "Detective Tapp",
            13 => "Adam Francis",
            14 => "Jeff Johansen",
            15 => "Jane Romero",
            16 => "Ashley J. Williams",
            17 => "Nancy Wheeler",
            18 => "Steve Harrington",
            19 => "Yui Kimura",
            20 => "Zarina Kassir",
            21 => "Cheryl Mason",
            22 => "Felix Richter",
            23 => "Élodie Rakoto",
            24 => "Yun-Jin Lee",
            25 => "Jill Valentine",
            26 => "Leon S. Kennedy",
            27 => "Mikaela Reid",
            28 => "Jonah Vasquez",
            29 => "Yoichi Asakawa",
            268435456 => "The Trapper",
            268435457 => "The Wraith",
            268435458 => "The Hillbilly",
            268435459 => "The Nurse",
            268435460 => "The Hag",
            268435461 => "The Shape",
            268435462 => "The Doctor",
            268435463 => "The Huntress",
            268435464 => "The Cannibal",
            268435465 => "The Nightmare",
            268435466 => "The Pig",
            268435467 => "The Clown",
            268435468 => "The Spirit",
            268435469 => "The Legion",
            268435470 => "The Plague",
            268435471 => "The Ghost Face",
            268435472 => "The Demogorgon",
            268435473 => "The Oni",
            268435474 => "The Deathslinger",
            268435475 => "The Executioner",
            268435476 => "The Blight",
            268435477 => "The Twins",
            268435478 => "The Trickster",
            268435479 => "The Nemesis",
            268435480 => "The Cenobite",
            268435481 => "The Artist",
            268435482 => "The Onryō",
            _ => "Unknown"
        };
    }

    public async Task RefreshAndNotifyAsync(NotificationSource source)
    {
        switch (source)
        {
            case NotificationSource.Epic:
                await RefreshGamesAsync().ConfigureAwait(false);
                break;
            case NotificationSource.Dbd:
                await RefreshPerksAsync().ConfigureAwait(false);
                break;
        }
        await NotifyChannelsAsync(source).ConfigureAwait(false);
    }

    private async Task RefreshGamesAsync()
    {
        var response = await _httpClient.GetStringAsync(Url).ConfigureAwait(false);
        CachedGames = EpicStore.FromJson(response).CurrentGames!;
    }

    private async Task RefreshPerksAsync()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36"
        );
        var response = await _httpClient.GetStringAsync(ShrineUrl).ConfigureAwait(false);
        var shrine = Shrines.FromJson(response);
        var perks = new List<Perk>();
        foreach (var perk in shrine.Perks)
        {
            var perkresponse = await _httpClient
                .GetStringAsync($"https://dbd.onteh.net.au/api/perkinfo?perk={perk.Id}")
                .ConfigureAwait(false);
            perks.Add(Perk.FromJson(perkresponse));
        }
        CachedPerks = perks;
    }

    private async Task NotifyChannelsAsync(NotificationSource source)
    {
        var channelIds =
            source is NotificationSource.Epic
                ? await _mongo.GetFreeGameNotificationChannelIds().ConfigureAwait(false)
                : await _mongo.GetShrineNotificationChannelIds().ConfigureAwait(false);
        var channels = channelIds
            .Select(id => _client.GetChannel(id) as ITextChannel)
            .Where(channel => channel is not null)
            .ToList();

        if (channels.Count == 0)
            return;

        foreach (var textChannel in channels)
        {
            await textChannel!
                .SendMessageAsync(
                    "test",
                    embeds: source is NotificationSource.Epic
                      ? CachedGames.ToEmbedArray()
                      : new[] { CachedPerks.ToEmbed() }
                )
                .ConfigureAwait(false);
        }
    }
}
