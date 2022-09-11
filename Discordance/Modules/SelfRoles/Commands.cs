using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Autocompletes;
using Discordance.Models;

namespace Discordance.Modules.SelfRoles;

[DefaultMemberPermissions(GuildPermission.ManageGuild)]
public class Commands : ModuleBase
{
    [SlashCommand("selfrole-create", "Creates a new button role message")]
    public async Task CreateButtonRoleMessageAsync(string title, string description)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(Color.Blue)
            .Build();

        var msg = await Context.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
        await DatabaseService
            .UpdateGuildConfig(
                Context.Guild.Id,
                x =>
                    x.SelfRoleMessages.Add(
                        new SelfRoleMessage(Context.Channel.Id, msg.Id, title, description)
                    )
            )
            .ConfigureAwait(false);
        var helpEmbed = new EmbedBuilder()
            .WithTitle("Message created!")
            .WithDescription(
                "To add roles use the **/br add** command, to remove a role you can use **/br remove** role!"
            )
            .WithColor(Color.Green)
            .Build();
        await FollowupAsync(embed: helpEmbed).ConfigureAwait(false);
    }

    [SlashCommand("selfrole-add", "Adds a role to the specified button role message.")]
    public async Task AddRoleToMessageAsync(
        [Summary("Message"), Autocomplete(typeof(SelfRoleMessageAutocompleteHandler))]
            string identifier,
        string title,
        IRole role,
        string emote,
        string? description = null
    )
    {
        await DeferAsync(true).ConfigureAwait(false);

        var split = identifier.Split(":");
        var channelId = ulong.Parse(split[0]);
        var messageId = ulong.Parse(split[1]);

        if (
            await Context.Guild
                .GetTextChannel(channelId)
                .GetMessageAsync(messageId)
                .ConfigureAwait(false)
            is not IUserMessage msg
        )
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Could not find message**");
            await FollowupAsync(embed: eb.Build()).ConfigureAwait(false);
            return;
        }

        var newConfig = await DatabaseService
            .UpdateGuildConfig(
                Context.Guild.Id,
                x =>
                    x.SelfRoleMessages[
                        x.SelfRoleMessages.FindIndex(y => y.MessageId == messageId)
                    ].AddRole(new SelfRole(role.Id, title, emote, description))
            )
            .ConfigureAwait(false);

        await msg.ModifyAsync(
                x =>
                    x.Components = newConfig.SelfRoleMessages
                        .First(x => x.MessageId == messageId)
                        .ToButtons()
            )
            .ConfigureAwait(false);
        var seb = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithDescription($"**Succesfully added {role.Mention} to the message**")
            .Build();
        await FollowupAsync(embed: seb).ConfigureAwait(false);
    }

    [SlashCommand("selfrole-remove", "Removes a role from the specified button role message.")]
    public async Task RemoveRoleFromMessageAsync(
        [Summary("Message"), Autocomplete(typeof(SelfRoleMessageAutocompleteHandler))]
            string identifier,
        IRole role
    )
    {
        await DeferAsync(true).ConfigureAwait(false);

        var split = identifier.Split(":");
        var channelId = ulong.Parse(split[0]);
        var messageId = ulong.Parse(split[1]);

        if (
            await Context.Guild
                .GetTextChannel(channelId)
                .GetMessageAsync(messageId)
                .ConfigureAwait(false)
            is not IUserMessage msg
        )
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Could not find message**");
            await FollowupAsync(embed: eb.Build()).ConfigureAwait(false);
            return;
        }

        var newConfig = await DatabaseService
            .UpdateGuildConfig(
                Context.Guild.Id,
                x =>
                    x.SelfRoleMessages[
                        x.SelfRoleMessages.FindIndex(y => y.MessageId == messageId)
                    ].RemoveRole(role)
            )
            .ConfigureAwait(false);

        await msg.ModifyAsync(
                x =>
                    x.Components = newConfig.SelfRoleMessages
                        .First(x => x.MessageId == messageId)
                        .ToButtons()
            )
            .ConfigureAwait(false);

        var seb = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithDescription("**Succesfully removed role from the message!.**")
            .Build();
        await FollowupAsync(embed: seb).ConfigureAwait(false);
    }
}
