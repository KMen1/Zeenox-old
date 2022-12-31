using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Lavalink4NET.Decoding;
using Zeenox.Autocompletes;
using Zeenox.Enums;
using Zeenox.Models;
using Zeenox.Preconditions;

namespace Zeenox.Modules.Music;

public class Commands : MusicBase
{
    [RequirePlayer]
    [RequireVoice]
    [SlashCommand("play-favorites", "Plays your favorite songs")]
    public async Task PlayFavoritesAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var user = await GetUserAsync().ConfigureAwait(false);
        if (user.Playlists.Count == 0)
        {
            await FollowupAsync("You don't have any favorite songs").ConfigureAwait(false);
            return;
        }

        var tracks = user.Playlists[0].Songs.Select(TrackDecoder.DecodeTrack).ToArray();
        await PlayAsync(tracks).ConfigureAwait(false);
        await FollowupAsync("Playing favorites").ConfigureAwait(false);
    }


    [RequireVoice]
    [RequirePlayer]
    [RequireDjRole]
    [SlashCommand("shuffle", "Shuffles the queue")]
    public async Task Shuffle()
    {
        await DeferAsync(true).ConfigureAwait(false);
        await ShuffleAsync().ConfigureAwait(false);
        await FollowupAsync("Shuffled the queue").ConfigureAwait(false);
    }

    [RequireVoice]
    [RequirePlayer]
    [RequireDjRole]
    [SlashCommand("removedupes", "Removes duplicate tracks from the queue")]
    public async Task RemoveDupes()
    {
        await DeferAsync(true).ConfigureAwait(false);
        await RemoveDupesAsync().ConfigureAwait(false);
        await FollowupAsync("Removed duplicate tracks from the queue").ConfigureAwait(false);
    }

    [RequireVoice]
    [RequireAllowedChannel]
    [RequireDjRole]
    [SlashCommand("play", "Plays a song")]
    public async Task PlayAsync(
        [Summary(
            "query",
            "Title of the song (must be more than 3 characters) or a link to a song or playlist"
        )]
        [Autocomplete(typeof(SearchAutocompleteHandler))]
        string query
    )
    {
        await DeferAsync(true).ConfigureAwait(false);
        var tracks = await SearchAsync(query, SearchMode.None).ConfigureAwait(false);
        if (tracks.Length == 0)
        {
            await FollowupAsync(ephemeral: true, embed: GetLocalizedEmbed("NoMatches", Color.Red))
                .ConfigureAwait(false);
            return;
        }

        var config = GetConfig();
        await CreatePlayerAsync().ConfigureAwait(false);

        var isPlaylist = tracks.Length > 1 && config.PlaylistAllowed;
        if (isPlaylist)
        {
            await PlayAsync(tracks).ConfigureAwait(false);
            await FollowupAsync(embed: GetLocalizedEmbed("AddedToQueue", Color.Orange)).ConfigureAwait(false);
            return;
        }

        await PlayAsync(tracks[0]).ConfigureAwait(false);
        await FollowupAsync(embed: GetLocalizedEmbed("AddedToQueue", Color.Orange)).ConfigureAwait(false);
    }

    [RequirePlayer]
    [RequireVoice]
    [RequireDjRole]
    [SlashCommand("leave", "Leaves the voice channel the bot is in")]
    public async Task DisconnectPlayerAsync()
    {
        var player = GetPlayer();
        await player.DisconnectAsync().ConfigureAwait(false);
        await RespondAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [RequirePlayer]
    [RequireVoice]
    [RequireDjRole]
    [SlashCommand("volume", "Sets the volume")]
    public async Task ChangeVolumeAsync(
        [Summary("volume", "Volume setting (1-100)")] [MinValue(1)] [MaxValue(100)]
        ushort volume
    )
    {
        var newVolume = await SetVolumeAsync(volume).ConfigureAwait(false);
        await RespondAsync(
                ephemeral: true,
                embed: GetLocalizedEmbed("SetVolume", Color.Green, newVolume.ToString()))
            .ConfigureAwait(false);
    }

    [RequirePlayer]
    [RequireVoice]
    [SlashCommand("queue", "Shows the current queue")]
    public async Task SendQueueAsync()
    {
        var player = GetPlayer();

        await DeferAsync(true).ConfigureAwait(false);
        var queue = player.Queue.ToList();

        var desc = new StringBuilder();
        var index = 0;
        foreach (var track in queue)
            desc.AppendLine(
                $"{index++}. [{track.Title}]({track.Uri}) | ({((TrackContext) track.Context!).Requester.Mention})"
            );

        var eb = new EmbedBuilder()
            .WithTitle(GetLocalized("Queue"))
            .WithColor(Color.Blue)
            .WithDescription(desc.ToString())
            .Build();
        await FollowupAsync(ephemeral: true, embed: eb).ConfigureAwait(false);
    }

    [RequirePlayer]
    [RequireVoice]
    [RequireDjRole]
    [SlashCommand("clearqueue", "Clears the current queue")]
    public async Task ClearAsync()
    {
        await ClearQueueAsync().ConfigureAwait(false);
        await RespondAsync("✅", ephemeral: true).ConfigureAwait(false);
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
                ephemeral: true,
                embed: GetLocalizedEmbed(config.Music.ExclusiveControl ? "ExclusiveEnabled" : "ExclusiveDisabled",
                    Color.Green))
            .ConfigureAwait(false);
    }

    [SlashCommand("toggle-dj", "If enabled, only users with a DJ role can control the player")]
    public async Task ToggleDjAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var config = await UpdateGuildConfigAsync(x => x.Music.DjOnly = !x.Music.DjOnly)
            .ConfigureAwait(false);

        await FollowupAsync(
                ephemeral: true,
                embed: GetLocalizedEmbed(config.Music.DjOnly ? "DjEnabled" : "DjDisabled", Color.Green))
            .ConfigureAwait(false);
    }

    [SlashCommand("add-dj-role", "Adds role to the list of DJ roles")]
    public async Task AddDjRoleAsync([Summary("role", "The role you want to add")] IRole role)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var config = GetConfig();

        if (config.DjRoleIds.Count >= 5)
        {
            await FollowupAsync(embed: GetLocalizedEmbed("DjLimitReached", Color.Red))
                .ConfigureAwait(false);
            return;
        }

        if (config.DjRoleIds.Contains(role.Id))
        {
            await FollowupAsync(embed: GetLocalizedEmbed("RoleAlreadyDj", Color.Red))
                .ConfigureAwait(false);
            return;
        }

        await UpdateGuildConfigAsync(x => x.Music.DjRoleIds.Add(role.Id)).ConfigureAwait(false);

        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    /*[SlashCommand("remove-dj-role", "Removes a role from the list of DJ roles")]
    public async Task RemoveDjRoleAsync(
        [Summary("role", "The role you want to remove")] [Autocomplete(typeof(DjRoleAutocompleteHandler))]
        string roleIdstr
    )
    {
        await DeferAsync(true).ConfigureAwait(false);
        var roleId = ulong.Parse(roleIdstr);
        await UpdateGuildConfigAsync(x => x.Music.DjRoleIds.Remove(roleId)).ConfigureAwait(false);

        await FollowupAsync(
                embed: GetLocalizedEmbed("dj_role_removed", Color.Green, Context.Guild.GetRole(roleId)?.Mention!),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }*/

    [SlashCommand("set-request-channel", "Sets the channel to receive song requests")]
    public async Task SetRequestChannelAsync(
        [Summary("channel", "This is the channel that will accept song requests")]
        ITextChannel channel
    )
    {
        await DeferAsync(true).ConfigureAwait(false);
        await UpdateGuildConfigAsync(x => x.Music.RequestChannelId = channel.Id)
            .ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
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
                ephemeral: true,
                embed: GetLocalizedEmbed(
                    config.Music.UseSponsorBlock ? "SponsorBlockEnabled" : "SponsorBlockDisabled", Color.Green))
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
            await FollowupAsync(embed: GetLocalizedEmbed("AllowedChannelLimitReached", Color.Red))
                .ConfigureAwait(false);
            return;
        }

        if (config.AllowedVoiceChannels.Contains(channel.Id))
        {
            await FollowupAsync(embed: GetLocalizedEmbed("ChannelAlreadyAllowed", Color.Red))
                .ConfigureAwait(false);
            return;
        }

        await UpdateGuildConfigAsync(x => x.Music.AllowedVoiceChannels.Add(channel.Id))
            .ConfigureAwait(false);

        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    /*[SlashCommand("remove-allowed-channel", "Removes a channel from the list of allowed channels")]
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
                embed: GetLocalizedEmbed("allowed_channel_removed", Color.Green,
                    Context.Guild.GetVoiceChannel(channelId)?.Mention!),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }*/
}