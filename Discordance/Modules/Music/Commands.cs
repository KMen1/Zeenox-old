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
    public async Task PlayAsync(string query)
    {
        await DeferAsync().ConfigureAwait(false);

        var (tracks, isPlaylist) = await AudioService
            .SearchAsync(query, Context.User)
            .ConfigureAwait(false);

        if (tracks is null)
        {
            await FollowupAsync("No matches", ephemeral: true).ConfigureAwait(false);
            return;
        }

        var config = await AudioService.GetConfig(Context.Guild.Id).ConfigureAwait(false);
        var firstTrack = tracks[0];

        var player = await AudioService
            .GetOrCreatePlayerAsync((SocketGuildUser)Context.User)
            .ConfigureAwait(false);

        if (player.IsPlaying)
        {
            if (isPlaylist && config.PlaylistAllowed)
            {
                await player
                    .PlayOrEnqueueAsync(Context.User, tracks, isPlaylist)
                    .ConfigureAwait(false);
                await FollowupAsync("added to queue").ConfigureAwait(false);
                return;
            }
            await player.PlayAsync(Context.User, firstTrack).ConfigureAwait(false);
            await FollowupAsync("added to queue", ephemeral: true).ConfigureAwait(false);
            return;
        }

        var msg = await FollowupAsync(
                embed: player.Embed(firstTrack),
                components: player.Components()
            )
            .ConfigureAwait(false);
        player.Message = msg;
        if (isPlaylist)
        {
            await player.PlayOrEnqueueAsync(Context.User, tracks, isPlaylist).ConfigureAwait(false);
            return;
        }
        await player.PlayAsync(firstTrack).ConfigureAwait(false);
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
    [SlashCommand("search", "Searches for a song")]
    public async Task SearchAsync(string query)
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

        var player = await AudioService.GetOrCreatePlayerAsync(user).ConfigureAwait(false);

        player.Message ??= msg;

        if (player.IsPlaying)
        {
            await msg.DeleteAsync().ConfigureAwait(false);
            await FollowupAsync("added to queue").ConfigureAwait(false);
        }
        track.Context = user;
        await player.PlayAsync(Context.User, track).ConfigureAwait(false);
    }

    [RequirePlayer]
    [RequireSameVoice]
    [RequireDjRole]
    [SlashCommand("volume", "Sets the volume")]
    public async Task ChangeVolumeAsync(
        [Summary("volume")] [MinValue(1)] [MaxValue(100)] ushort volume
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
                $":{index++}. [`{track.Title}`]({track.Source}) | ({((IUser)track.Context!).Mention})"
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

    [SlashCommand("exclusive", "Toggles exclusive control mode")]
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

    [SlashCommand("djonly", "Toggles DJ only mode")]
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

    [SlashCommand("add-dj-role", "Adds a DJ role")]
    public async Task AddDjRoleAsync(IRole role)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var config = await GetConfig().ConfigureAwait(false);

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

    [SlashCommand("remove-dj-role", "Removes a DJ role")]
    public async Task RemoveDjRoleAsync(
        [Summary("role"), Autocomplete(typeof(DjRoleAutocompleteHandler))] string roleIdstr
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

    [SlashCommand("setrequestchannel", "Sets the channel to receive song requests")]
    public async Task SetRequestChannelAsync(SocketTextChannel channel)
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

    [SlashCommand("toggle-sponsorblock", "Toggles the sponsor block")]
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
                player.GetCategories().Add(SegmentCategory.OfftopicMusic);
            }
            else
            {
                player.GetCategories().Clear();
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
    public async Task AddAllowedChannelAsync(IVoiceChannel channel)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var config = await GetConfig().ConfigureAwait(false);

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
        [Summary("channel"), Autocomplete(typeof(AllowedChannelAutocompleteHandler))]
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
        if (player.Message is not null)
        {
            var preMsg = await player.Message.Channel
                .GetMessageAsync(player.Message.Id)
                .ConfigureAwait(false);
            if (preMsg is not null)
                await preMsg.DeleteAsync().ConfigureAwait(false);
        }
        var msg = await Context.Channel
            .SendMessageAsync(embed: player.Embed(), components: player.Components())
            .ConfigureAwait(false);
        player.Message = msg;

        await RespondAsync("Sent now playing message!", ephemeral: true).ConfigureAwait(false);
    }
}
