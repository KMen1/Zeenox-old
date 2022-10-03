using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Discordance.Autocompletes;
using Discordance.Extensions;
using Discordance.Preconditions;

namespace Discordance.Modules.Music;

public class Commands : MusicBase
{
    [RequireVoice]
    [RequireAllowedChannel]
    [RequireDjRole]
    [SlashCommand("play", "Plays a song")]
    public async Task PlayAsync(
        [Summary(
            "query",
            "Name or the url of the song (accepts Youtube, SoundCloud, Spotify links)"
        )]
        string query
    )
    {
        await DeferAsync(true).ConfigureAwait(false);
        var tracks = await SearchAsync(query).ConfigureAwait(false);
        if (tracks is null)
        {
            await FollowupAsync(GetLocalized("no_matches"), ephemeral: true).ConfigureAwait(false);
            return;
        }

        var config = GetConfig();
        var (player, isCreated) = await GetOrCreatePlayerAsync().ConfigureAwait(false);

        var playlist = tracks.Length > 1 && config.PlaylistAllowed;

        switch (!isCreated)
        {
            case true when playlist:
            {
                await PlayAsync(tracks).ConfigureAwait(false);
                await FollowupAsync(GetLocalized("added_to_queue")).ConfigureAwait(false);
                return;
            }
            case true:
            {
                await PlayAsync(tracks[0]).ConfigureAwait(false);
                await FollowupAsync(GetLocalized("added_to_queue")).ConfigureAwait(false);
                return;
            }
            case false when playlist:
            {
                var (embed, components) = await PlayAsync(tracks).ConfigureAwait(false);

                var msg = await Context.Channel
                    .SendMessageAsync(embed: embed, components: components)
                    .ConfigureAwait(false);
                player.SetMessage(msg);
                await FollowupAsync(GetLocalized("player_started")).ConfigureAwait(false);
                return;
            }
            case false:
            {
                var (embed, components) = await PlayAsync(tracks[0]).ConfigureAwait(false);
                var msg = await Context.Channel
                    .SendMessageAsync(embed: embed, components: components)
                    .ConfigureAwait(false);
                player.SetMessage(msg);
                await FollowupAsync(GetLocalized("player_started")).ConfigureAwait(false);
                return;
            }
        }
    }

    [RequirePlayer]
    [RequireSameVoice]
    [RequireDjRole]
    [SlashCommand("leave", "Leaves the voice channel the bot is in")]
    public async Task DisconnectPlayerAsync()
    {
        var player = GetPlayer();
        await player.DisconnectAsync().ConfigureAwait(false);

        await RespondAsync(
                embed: new EmbedBuilder()
                    .WithDescription(
                        GetLocalized("left_channel").Format(player.VoiceChannel.Mention)
                    )
                    .WithColor(Color.Green)
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [RequireVoice]
    [RequireAllowedChannel]
    [RequireDjRole]
    [SlashCommand("search", "Searches for a song on Youtube")]
    public async Task SearchSongAsync([Summary("query", "Title of the song")] string query)
    {
        await DeferAsync().ConfigureAwait(false);
        var tracks = await SearchAsync(query).ConfigureAwait(false);
        if (tracks is null)
        {
            var eeb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription(GetLocalized("no_matches"))
                .Build();
            await FollowupAsync(embed: eeb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        var availableTracks = tracks.Take(10).ToList();

        var comp = new ComponentBuilder();
        var menu = new SelectMenuBuilder().WithCustomId("search").WithMinValues(1).WithMaxValues(1);
        var counter = 0;
        foreach (var track in availableTracks)
        {
            var emoji = counter switch
            {
                0 => "1️⃣",
                1 => "2️⃣",
                2 => "3️⃣",
                3 => "4️⃣",
                4 => "5️⃣",
                5 => "6️⃣",
                6 => "7️⃣",
                7 => "8️⃣",
                8 => "9️⃣",
                9 => "🔟"
            };
            menu.AddOption($"{track.Title}", $"{track.TrackIdentifier}", emote: new Emoji(emoji));
            counter++;
        }

        comp.WithSelectMenu(menu);

        var eb = new EmbedBuilder()
            .WithTitle(GetLocalized("select_song"))
            .WithColor(Color.Blue)
            .Build();
        await FollowupAsync(embed: eb, components: comp.Build()).ConfigureAwait(false);
    }

    /*[RequireVoice]
    [RequireDjRole]
    [ComponentInteraction("search")]
    public async Task PlaySearchAsync(params string[] trackIdentifiers)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var user = (SocketGuildUser)Context.User;
        var msg = ((SocketMessageComponent)Context.Interaction).Message;
        var identifier = trackIdentifiers[0];
        var track = await AudioService
            .GetTrackAsync($"https://www.youtube.com/watch?v={identifier}")
            .ConfigureAwait(false);

        track.Context = user;
        await msg.DeleteAsync().ConfigureAwait(false);
        var player = await AudioService.GetOrCreatePlayerAsync(user).ConfigureAwait(false);
        if (player.IsPlaying)
        {
            await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithTitle("Added To Queue")
                        .WithColor(Color.Green)
                        .Build()
                )
                .ConfigureAwait(false);
            await AudioService
                .PlayAsync(Context.Guild.Id, Context.User, track)
                .ConfigureAwait(false);
        }

        player.SetMessage(
            await Context.Channel
                .SendMessageAsync(
                    embed: player.GetNowPlayingEmbed(),
                    components: player.GetMessageComponents()
                )
                .ConfigureAwait(false)
        );
    }*/

    [RequirePlayer]
    [RequireSameVoice]
    [RequireDjRole]
    [SlashCommand("volume", "Sets the volume")]
    public async Task ChangeVolumeAsync(
        [Summary("volume", "Volume setting (1-100)")] [MinValue(1)] [MaxValue(100)]
        ushort volume
    )
    {
        var newVolume = await SetVolumeAsync(volume).ConfigureAwait(false);
        await RespondAsync(
                embed: new EmbedBuilder()
                    .WithDescription(GetLocalized("set_volume").Format(newVolume))
                    .WithColor(Color.Green)
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [RequirePlayer]
    [RequireSameVoice]
    [SlashCommand("queue", "Shows the current queue")]
    public async Task SendQueueAsync()
    {
        var player = GetPlayer();

        await DeferAsync().ConfigureAwait(false);
        var queue = player.Queue.ToList();

        var desc = new StringBuilder();
        var index = 0;
        foreach (var track in queue)
            desc.AppendLine(
                $":{index++}. [`{track.Title}`]({track.Uri}) | ({((IUser) track.Context!).Mention})"
            );
        var eb = new EmbedBuilder()
            .WithTitle(GetLocalized("queue"))
            .WithColor(Color.Blue)
            .WithDescription(desc.ToString())
            .Build();
        await FollowupAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }

    [RequirePlayer]
    [RequireSameVoice]
    [RequireDjRole]
    [SlashCommand("clearqueue", "Clears the current queue")]
    public async Task ClearAsync()
    {
        await ClearQueueAsync();
        await RespondAsync(
                embed: new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription(GetLocalized("queue_cleared"))
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [SlashCommand(
        "exclusive",
        "If enabled, only the person who added the current song can control the player"
    )]
    public async Task ToggleExclusiveControlAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var config = await UpdateGuildConfigAsync(
                x => x.Music.ExclusiveControl = !x.Music.ExclusiveControl
            )
            .ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithTitle(
                        config.Music.ExclusiveControl
                            ? GetLocalized("exclusive_enabled")
                            : GetLocalized("exclusive_disabled")
                    )
                    .WithColor(config.Music.ExclusiveControl ? Color.Green : Color.Red)
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [SlashCommand("djonly", "If enabled, only users with a DJ role can control the player")]
    public async Task ToggleDjOnlyAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var config = await UpdateGuildConfigAsync(x => x.Music.DjOnly = !x.Music.DjOnly)
            .ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithTitle(
                        config.Music.DjOnly
                            ? GetLocalized("dj_enabled")
                            : GetLocalized("dj_disabled")
                    )
                    .WithColor(config.Music.DjOnly ? Color.Green : Color.Red)
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [SlashCommand("add-dj-role", "Adds role to the list of DJ roles")]
    public async Task AddDjRoleAsync([Summary("role", "The role you want to add")] IRole role)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var config = GetConfig();

        if (config.DjRoleIds.Count >= 5)
        {
            await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithTitle(GetLocalized("dj_limit_reached"))
                        .WithColor(Color.Red)
                        .Build()
                )
                .ConfigureAwait(false);
            return;
        }

        if (config.DjRoleIds.Contains(role.Id))
        {
            await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithDescription(GetLocalized("role_already_dj"))
                        .WithColor(Color.Red)
                        .Build()
                )
                .ConfigureAwait(false);
            return;
        }

        await UpdateGuildConfigAsync(x => x.Music.DjRoleIds.Add(role.Id)).ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithDescription(GetLocalized("dj_role_added").Format(role.Mention))
                    .WithColor(Color.Green)
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [SlashCommand("remove-dj-role", "Removes a role from the list of DJ roles")]
    public async Task RemoveDjRoleAsync(
        [Summary("role", "The role you want to remove")] [Autocomplete(typeof(DjRoleAutocompleteHandler))]
        string roleIdstr
    )
    {
        await DeferAsync(true).ConfigureAwait(false);
        var roleId = ulong.Parse(roleIdstr);
        await UpdateGuildConfigAsync(x => x.Music.DjRoleIds.Remove(roleId)).ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithDescription(
                        GetLocalized("dj_role_removed")
                            .Format(Context.Guild.GetRole(roleId)?.Mention)
                    )
                    .WithColor(Color.Green)
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [SlashCommand("set-request-channel", "Sets the channel to receive song requests")]
    public async Task SetRequestChannelAsync(
        [Summary("channel", "This is the channel that will accept song requests")]
        SocketTextChannel channel
    )
    {
        await DeferAsync(true).ConfigureAwait(false);

        await UpdateGuildConfigAsync(x => x.Music.RequestChannelId = channel.Id)
            .ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithDescription(GetLocalized("request_channel_set").Format(channel.Mention))
                    .WithColor(Color.Green)
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [SlashCommand("toggle-sponsorblock", "Toggles the sponsorblock")]
    public async Task ToggleSponsorBlockAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var config = await UpdateGuildConfigAsync(
                x => x.Music.UseSponsorBlock = !x.Music.UseSponsorBlock
            )
            .ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithTitle(
                        config.Music.UseSponsorBlock
                            ? GetLocalized("sponsorblock_enabled")
                            : GetLocalized("sponsorblock_disabled")
                    )
                    .WithColor(config.Music.UseSponsorBlock ? Color.Green : Color.Red)
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [SlashCommand("add-allowed-channel", "Adds a channel to the list of allowed channels")]
    public async Task AddAllowedChannelAsync(
        [Summary("channel", "The channel you want to add to the list af allowed channels")]
        IVoiceChannel channel
    )
    {
        await DeferAsync(true).ConfigureAwait(false);
        var config = GetConfig();

        if (config.AllowedVoiceChannels.Count >= 25)
        {
            await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithTitle(GetLocalized("allowed_channel_limit_reached"))
                        .WithColor(Color.Red)
                        .Build()
                )
                .ConfigureAwait(false);
            return;
        }

        if (config.AllowedVoiceChannels.Contains(channel.Id))
        {
            await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithDescription(GetLocalized("channel_already_allowed"))
                        .WithColor(Color.Red)
                        .Build()
                )
                .ConfigureAwait(false);
            return;
        }

        await UpdateGuildConfigAsync(x => x.Music.AllowedVoiceChannels.Add(channel.Id))
            .ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithDescription(GetLocalized("allowed_channel_added").Format(channel.Mention))
                    .WithColor(Color.Green)
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [SlashCommand("remove-allowed-channel", "Removes a DJ role")]
    public async Task RemoveChannelAsync(
        [Summary("channel", "The channel you want to remove from the list of allowed channels")]
        [Autocomplete(typeof(AllowedChannelAutocompleteHandler))]
        string channelIdstr
    )
    {
        await DeferAsync(true).ConfigureAwait(false);
        var channelId = ulong.Parse(channelIdstr);
        await UpdateGuildConfigAsync(x => x.Music.AllowedVoiceChannels.Remove(channelId))
            .ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithDescription(
                        GetLocalized("allowed_channel_removed")
                            .Format(Context.Guild.GetVoiceChannel(channelId)?.Mention)
                    )
                    .WithColor(Color.Green)
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }
}