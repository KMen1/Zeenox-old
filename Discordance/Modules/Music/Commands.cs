using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Discordance.Autocompletes;
using Discordance.Preconditions;
using Lavalink4NET.Integrations.SponsorBlock;
using Lavalink4NET.Player;

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

        var (tracks, isPlaylist) = await AudioService
            .SearchAsync(query, Context.User)
            .ConfigureAwait(false);

        if (tracks is null)
        {
            await FollowupAsync(
                    Localization.GetMessage(Context.Guild.Id, "no_matches"),
                    ephemeral: true
                )
                .ConfigureAwait(false);
            return;
        }

        var config = GetConfig();
        var firstTrack = tracks[0];

        var player = await AudioService
            .GetOrCreatePlayerAsync((SocketGuildUser)Context.User)
            .ConfigureAwait(false);

        if (player.IsPlaying)
        {
            if (isPlaylist && config.PlaylistAllowed)
            {
                await player.PlayPlaylistAsync(Context.User, tracks).ConfigureAwait(false);
                await FollowupAsync(Localization.GetMessage(Context.Guild.Id, "added_to_queue"))
                    .ConfigureAwait(false);
                return;
            }
            await player.PlayAsync(Context.User, firstTrack).ConfigureAwait(false);
            await FollowupAsync(
                    Localization.GetMessage(Context.Guild.Id, "added_to_queue"),
                    ephemeral: true
                )
                .ConfigureAwait(false);
            return;
        }

        var msg = await Context.Channel
            .SendMessageAsync(
                embed: player.GetNowPlayingEmbed(firstTrack),
                components: player.GetMessageComponents()
            )
            .ConfigureAwait(false);
        player.SetMessage(msg);
        if (isPlaylist && config.PlaylistAllowed)
        {
            await player.PlayPlaylistAsync(Context.User, tracks).ConfigureAwait(false);
            return;
        }
        await player.PlayAsync(firstTrack).ConfigureAwait(false);

        await FollowupAsync(Localization.GetMessage(Context.Guild.Id, "player_started"));
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
                    .WithDescription($"**Successfully left {player.VoiceChannel.Mention}**")
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
    public async Task SearchAsync([Summary("query", "Title of the song")] string query)
    {
        if (Uri.IsWellFormedUriString(query, UriKind.Absolute))
        {
            await PlayAsync(query).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        var (tracks, _) = await AudioService.SearchAsync(query, Context.User).ConfigureAwait(false);
        if (tracks is null)
        {
            var eeb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Not matches, please try again!**")
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

        var eb = new EmbedBuilder().WithTitle("Please select a song").WithColor(Color.Blue).Build();
        await FollowupAsync(embed: eb, components: comp.Build()).ConfigureAwait(false);
    }

    [RequireVoice]
    [RequireDjRole]
    [ComponentInteraction("search")]
    public async Task PlaySearchAsync(params string[] trackIdentifiers)
    {
        await DeferAsync().ConfigureAwait(false);
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
            await player.PlayAsync(Context.User, track).ConfigureAwait(false);
        }

        player.SetMessage(
            await Context.Channel
                .SendMessageAsync(
                    embed: player.GetNowPlayingEmbed(),
                    components: player.GetMessageComponents()
                )
                .ConfigureAwait(false)
        );
    }

    [RequirePlayer]
    [RequireSameVoice]
    [RequireDjRole]
    [SlashCommand("volume", "Sets the volume")]
    public async Task ChangeVolumeAsync(
        [Summary("volume", "Volume setting (1-100)")] [MinValue(1)] [MaxValue(100)] ushort volume
    )
    {
        var player = GetPlayer();

        await player.SetVolumeAsync(Context.User, volume).ConfigureAwait(false);
        await RespondAsync(
                embed: new EmbedBuilder()
                    .WithDescription(
                        $"**Successfully set volume to {player.Volume.ToString(CultureInfo.InvariantCulture)}**"
                    )
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
        {
            desc.AppendLine(
                $":{index++}. [`{track.Title}`]({track.Uri}) | ({((IUser)track.Context!).Mention})"
            );
        }
        var eb = new EmbedBuilder()
            .WithTitle("Queue")
            .WithColor(Color.Blue)
            .WithDescription(desc.ToString())
            .Build();
        await FollowupAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }

    [RequirePlayer]
    [RequireSameVoice]
    [RequireDjRole]
    [SlashCommand("clearqueue", "Clears the current queue")]
    public async Task ClearQueueAsync()
    {
        var player = GetPlayer();
        var clearedAmount = player.ClearQueue(Context.User);
        await RespondAsync(
                embed: new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription($"**Removed {clearedAmount} tracks from the queue**")
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
        var config = await DatabaseService
            .UpdateGuildConfig(
                Context.Guild.Id,
                x => x.Music.ExclusiveControl = !x.Music.ExclusiveControl
            )
            .ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithTitle(
                        config.Music.ExclusiveControl
                          ? "Exclusive Control Enabled"
                          : "Exclusive Control Disabled"
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
        var config = await DatabaseService
            .UpdateGuildConfig(Context.Guild.Id, x => x.Music.DjOnly = !x.Music.DjOnly)
            .ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithTitle(
                        config.Music.DjOnly ? "DJ Only Mode Enabled" : "DJ Only Mode Disabled"
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
                        .WithTitle("You can only have 5 DJ roles")
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
                        .WithDescription($"**{role.Mention} is already configured as a DJ role!**")
                        .WithColor(Color.Red)
                        .Build()
                )
                .ConfigureAwait(false);
            return;
        }

        await DatabaseService
            .UpdateGuildConfig(Context.Guild.Id, x => x.Music.DjRoleIds.Add(role.Id))
            .ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithDescription($"**Added {role.Mention} to the list of DJ roles!**")
                    .WithColor(Color.Green)
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [SlashCommand("remove-dj-role", "Removes a role from the list of DJ roles")]
    public async Task RemoveDjRoleAsync(
        [
            Summary("role", "The role you want to remove"),
            Autocomplete(typeof(DjRoleAutocompleteHandler))
        ]
            string roleIdstr
    )
    {
        await DeferAsync(true).ConfigureAwait(false);
        var roleId = ulong.Parse(roleIdstr);
        await DatabaseService
            .UpdateGuildConfig(Context.Guild.Id, x => x.Music.DjRoleIds.Remove(roleId))
            .ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithDescription(
                        $"**Removed {Context.Guild.GetRole(roleId)?.Mention} from the list of DJ roles!**"
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

        await DatabaseService
            .UpdateGuildConfig(Context.Guild.Id, x => x.Music.RequestChannelId = channel.Id)
            .ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithDescription($"**Set the request channel to {channel.Mention}**")
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
        var config = await DatabaseService
            .UpdateGuildConfig(
                Context.Guild.Id,
                x => x.Music.UseSponsorBlock = !x.Music.UseSponsorBlock
            )
            .ConfigureAwait(false);

        if (AudioService.IsPlaying(Context.Guild.Id, out var player))
        {
            if (config.Music.UseSponsorBlock)
            {
                player!.GetCategories().Add(SegmentCategory.OfftopicMusic);
            }
            else
            {
                player!.GetCategories().Clear();
            }
        }

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithTitle(
                        config.Music.UseSponsorBlock
                          ? "Sponsor Block Enabled"
                          : "Sponsor Block Disabled"
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
                        .WithTitle("You can only have 25 allowed channels")
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
                        .WithDescription(
                            $"**{channel.Mention} is already in the list of allowed channels!**"
                        )
                        .WithColor(Color.Red)
                        .Build()
                )
                .ConfigureAwait(false);
            return;
        }

        await DatabaseService
            .UpdateGuildConfig(Context.Guild.Id, x => x.Music.AllowedVoiceChannels.Add(channel.Id))
            .ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithDescription(
                        $"**Added {channel.Mention} to the list of allowed channels!**"
                    )
                    .WithColor(Color.Green)
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [SlashCommand("remove-allowed-channel", "Removes a DJ role")]
    public async Task RemoveChannelAsync(
        [
            Summary("channel", "The channel you want to remove from the list of allowed channels"),
            Autocomplete(typeof(AllowedChannelAutocompleteHandler))
        ]
            string channelIdstr
    )
    {
        await DeferAsync(true).ConfigureAwait(false);
        var channelId = ulong.Parse(channelIdstr);
        await DatabaseService
            .UpdateGuildConfig(
                Context.Guild.Id,
                x => x.Music.AllowedVoiceChannels.Remove(channelId)
            )
            .ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithDescription(
                        $"**Removed {Context.Guild.GetVoiceChannel(channelId)?.Mention} from the list of allowed channels!**"
                    )
                    .WithColor(Color.Green)
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [RequirePlayer]
    [SlashCommand("nowplaying", "Sends the now playing message")]
    public async Task SendNowPlayingMessageAsync()
    {
        var player = GetPlayer();

        var playerMsg = await player.TextChannel
            .GetMessageAsync(player.Message.Id)
            .ConfigureAwait(false);
        if (playerMsg is not null)
            await playerMsg.DeleteAsync().ConfigureAwait(false);

        player.SetMessage(
            await Context.Channel
                .SendMessageAsync(
                    embed: player.GetNowPlayingEmbed(),
                    components: player.GetMessageComponents()
                )
                .ConfigureAwait(false)
        );

        await RespondAsync("Sent now playing message!", ephemeral: true).ConfigureAwait(false);
    }
}
