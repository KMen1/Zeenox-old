using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discordance.Services;

namespace Discordance.Modules.TemporaryChannels;

public class TempBase : ModuleBase
{
    public TemporaryChannelService TempChannelService { get; set; } = null!;

    protected Task<bool> LockChannelAsync()
    {
        return TempChannelService.LockChannelAsync(Context.User.Id);
    }

    protected Task<bool> UnlockChannelAsync()
    {
        return TempChannelService.UnlockChannelAsync(Context.User.Id);
    }

    protected Task<bool> LimitChannelAsync(int limit)
    {
        return TempChannelService.LimitChannelAsync(Context.User.Id, limit);
    }

    protected Task<bool> RenameChannelAsync(string name)
    {
        return TempChannelService.RenameChannelAsync(Context.User.Id, name);
    }

    protected Task<bool> KickUsersAsync(IEnumerable<ulong> users)
    {
        return TempChannelService.KickUsersAsync(Context.User.Id, users);
    }

    protected Task<bool> BanUsersAsync(IEnumerable<ulong> users)
    {
        return TempChannelService.BanUsersAsync(Context.User.Id, users);
    }

    protected IEnumerable<IUser> GetBannedUsers()
    {
        return TempChannelService.GetBannedUsers(Context.User.Id);
    }

    protected Task<bool> UnbanUsersAsync(IEnumerable<ulong> users)
    {
        return TempChannelService.UnbanUsersAsync(Context.User.Id, users);
    }

    protected async Task UpdateMessageAsync()
    {
        if (!TempChannelService.GetTempChannel(Context.User.Id, out var channelId))
            return;

        var channel = Context.Guild.GetVoiceChannel(channelId);

        var isLocked = channel.PermissionOverwrites.Any(x =>
            x.TargetType == PermissionTarget.Role && x.TargetId == Context.Guild.EveryoneRole.Id &&
            x.Permissions.Connect == PermValue.Deny);

        var embed = new EmbedBuilder()
            .WithTitle(channel.Name)
            .AddField("🎙️ Bitrate", $"`{channel.Bitrate / 1000} kbps`", true)
            .AddField("👥 User limit", $"`{channel.UserLimit.ToString()}`", true)
            .AddField("🔒 Who can join?", isLocked ? "`Only moved`" : "`Everyone`", true)
            .WithColor(Color.Green)
            .Build();

        var buttons = new ComponentBuilder()
            .WithButton(
                isLocked ? "Unlock" : "Lock",
                isLocked ? "unlock" : "lock",
                isLocked ? ButtonStyle.Success : ButtonStyle.Danger,
                isLocked ? new Emoji("🔓") : new Emoji("🔒"))
            .WithButton("Ban", "ban", ButtonStyle.Danger, new Emoji("🚫"))
            .WithButton("Kick", "kick", ButtonStyle.Danger, new Emoji("👢"))
            .WithButton("Limit", "limit", ButtonStyle.Primary, new Emoji("👥"))
            .WithButton("Rename", "rename", ButtonStyle.Primary, new Emoji("📝"))
            .Build();

        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = embed;
            x.Components = buttons;
        }).ConfigureAwait(false);
    }
}