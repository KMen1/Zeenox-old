using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Discordance.Preconditions;

namespace Discordance.Modules.TemporaryChannels;

[RequireTempChannel]
public class Interactions : TempBase
{
    [ComponentInteraction("lock")]
    public async Task LockChannelInteractionAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        await LockChannelAsync().ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
        await FollowupAsync("Channel locked.", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("unlock")]
    public async Task UnlockChannelInteractionAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        await UnlockChannelAsync().ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
        await FollowupAsync("Channel unlocked.", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("ban")]
    public async Task BanUserAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var channel = ((SocketGuildUser) Context.User).VoiceChannel;
        var selectMenu = new SelectMenuBuilder("banselect",
            channel.ConnectedUsers.Select(x => new SelectMenuOptionBuilder(x.Username, x.Id.ToString())).ToList());
        await FollowupAsync(components: new ComponentBuilder().WithSelectMenu(selectMenu).Build(), ephemeral: true)
            .ConfigureAwait(false);
    }

    [ComponentInteraction("banselect")]
    public async Task BanUsersInteractionAsync(params string[] userIds)
    {
        await DeferAsync(true).ConfigureAwait(false);
        await BanUsersAsync(userIds.Select(ulong.Parse)).ConfigureAwait(false);
        await FollowupAsync("Users banned.", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("unban")]
    public async Task UnbanUsersInteractionAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var selectMenu = new SelectMenuBuilder("unbanselect",
            GetBannedUsers().Select(x => new SelectMenuOptionBuilder(x.Username, x.Id.ToString())).ToList());
        await FollowupAsync(components: new ComponentBuilder().WithSelectMenu(selectMenu).Build(), ephemeral: true)
            .ConfigureAwait(false);
    }

    [ComponentInteraction("unbanselect")]
    public async Task UnbanUsersInteractionAsync(params string[] userIds)
    {
        await DeferAsync(true).ConfigureAwait(false);
        await UnbanUsersAsync(userIds.Select(ulong.Parse)).ConfigureAwait(false);
        await FollowupAsync("Users banned.", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("kick")]
    public async Task KickUsersInteractionAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var channel = ((SocketGuildUser) Context.User).VoiceChannel;
        var selectMenu = new SelectMenuBuilder("kickselect",
            channel.ConnectedUsers.Select(x => new SelectMenuOptionBuilder(x.Username, x.Id.ToString())).ToList());
        await FollowupAsync("select", components: new ComponentBuilder().WithSelectMenu(selectMenu).Build(),
            ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("kickselect")]
    public async Task KickUsersInteractionAsync(params string[] userIds)
    {
        await DeferAsync(true).ConfigureAwait(false);
        await KickUsersAsync(userIds.Select(ulong.Parse)).ConfigureAwait(false);
        await FollowupAsync("Users kicked.", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("limit:*")]
    public async Task LimitChannelInteractionAsync(int limit)
    {
        await DeferAsync(true).ConfigureAwait(false);
        await LimitChannelAsync(limit).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
        await FollowupAsync($"Channel limit set to {limit}.", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("rename")]
    public async Task SendRenameModalAsync()
    {
        var modal = new ModalBuilder()
            .WithTitle("Rename Channel")
            .WithCustomId("renamemodal")
            .AddTextInput("What should the new name be?", "newname", TextInputStyle.Short, "My super channel", 1, 25,
                true)
            .Build();
        await RespondWithModalAsync(modal).ConfigureAwait(false);
    }

    [ModalInteraction("renamemodal")]
    public async Task RenameChannelInteractionAsync(RenameModal modal)
    {
        await DeferAsync(true).ConfigureAwait(false);
        await RenameChannelAsync(modal.NewName).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
        await FollowupAsync("Channel renamed.", ephemeral: true).ConfigureAwait(false);
    }

    public class RenameModal : IModal
    {
        [ModalTextInput("newname")] public string NewName { get; set; } = null!;
        public string Title => string.Empty;
    }
}