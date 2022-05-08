using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Extensions;
using KBot.Models;

namespace KBot.Modules.Moderation;

[DefaultMemberPermissions(GuildPermission.SendMessages)]
public class UserCommands : SlashModuleBase
{
    [UserCommand("Check Warns")]
    public async Task CheckWarnsAsync(SocketGuildUser user)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var warns = (await Mongo.GetWarnsAsync(user).ConfigureAwait(false)).ToList();
        if (warns.Count == 0)
        {
            await FollowupWithEmbedAsync(
                    Color.Gold,
                    "😎 Good job!",
                    $"{user.Mention} has no warns!"
                )
                .ConfigureAwait(false);
            return;
        }

        var warnString = warns.Aggregate(
            "",
            (current, warn) =>
                current
                + $"`{warn.Id}`:`{warn.Date.ToString(CultureInfo.InvariantCulture)}` **By:** {Context.Client.GetUser(warn.GivenById).Mention} - **Reason:** `{warn.Reason}`\n"
        );
        await FollowupWithEmbedAsync(
                Color.Orange,
                $"{user.Username} has {warns.Count} warns",
                warnString,
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [SlashCommand("warninfo", "Get more information about a warn.")]
    public async Task WarnInfoAsync(string warnId)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var warn = await Mongo.GetWarnAsync(warnId).ConfigureAwait(false);
        if (warn == null)
        {
            await FollowupWithEmbedAsync(Color.Red, $"Warn with id({warnId}) does not exist", "")
                .ConfigureAwait(false);
            return;
        }

        var moderator = Context.Client.GetUser(warn.GivenById);
        var user = Context.Client.GetUser(warn.GivenToId);
        var eb = new EmbedBuilder()
            .WithTitle($"Details of warn: {warn.Id}")
            .WithColor(Color.Orange)
            .AddField("Given By", $"{moderator.Mention}", true)
            .AddField("Given To", $"{user.Mention}", true)
            .AddField("Date", $"{warn.Date.ToString(CultureInfo.InvariantCulture)}", true)
            .AddField("Reason", $"```{warn.Reason}```")
            .WithCurrentTimestamp()
            .Build();
        await FollowupAsync(embed: eb).ConfigureAwait(false);
    }

    [SlashCommand("appeal", "Appeal a warn to the mod team.")]
    public async Task AppealAsync(string warnId)
    {
        await DeferAsync().ConfigureAwait(false);
        var warn = await Mongo.GetWarnAsync(warnId).ConfigureAwait(false);
        if (warn is null)
        {
            await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithDescription("**Warn with that id doesn't exist!**")
                        .WithColor(Color.Red)
                        .Build()
                )
                .ConfigureAwait(false);
            return;
        }

        if (warn.GivenToId != Context.User.Id)
        {
            await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithDescription("**You can't appeal another person's warn!**")
                        .WithColor(Color.Green)
                        .Build()
                )
                .ConfigureAwait(false);
            return;
        }

        var modal = new ModalBuilder()
            .WithTitle("Appeal a warn")
            .WithCustomId($"appeal:{warn.Id}")
            .AddTextInput(
                "Whats your reason for appealing?",
                "appeal-reason",
                TextInputStyle.Paragraph,
                "False warn...",
                required: true
            )
            .AddTextInput(
                "Why do you think we should accept your appeal?",
                "appeal-acceptreason",
                TextInputStyle.Paragraph,
                "Rule breaking",
                required: true
            );
        await RespondWithModalAsync(modal.Build()).ConfigureAwait(false);
    }

    [ModalInteraction("appeal:*")]
    public async Task HandleAppealAsync(string warnId, AppealModal submission)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var warn = await Mongo.GetWarnAsync(warnId).ConfigureAwait(false);
        if (warn is null)
        {
            await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithDescription("**Something went wrong... Please try again!**")
                        .WithColor(Color.Red)
                        .Build(),
                    ephemeral: true
                )
                .ConfigureAwait(false);
            return;
        }
        var admin = Context.Guild.GetUser(warn.GivenById);
        var user = Context.Guild.GetUser(warn.GivenToId);

        var config = await Mongo.GetGuildConfigAsync(Context.Guild).ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithAuthor($"{user.Username}{user.Discriminator}", user.GetAvatarUrl())
            .WithTitle($"Appeal #{warn.Id}")
            .WithColor(Color.Orange)
            .AddField("Status", "**Pending**")
            .AddField("Who gave the punishment?", admin.Mention)
            .AddField("Whats your reason for appealing?", submission.AppealReason)
            .AddField("Why do you think we should accept your appeal?", submission.AcceptReason)
            .Build();
        var comp = new ComponentBuilder()
            .WithButton(
                "Accept",
                $"appeal-accept:{warnId}:{Context.User.Id}:{admin.Id}",
                ButtonStyle.Success,
                new Emoji("✅")
            )
            .WithButton(
                "Deny",
                $"appeal-decline:{warnId}:{Context.User.Id}:{admin.Id}",
                ButtonStyle.Danger,
                new Emoji("❌")
            )
            .Build();
        await Context.Guild
            .GetTextChannel(config.AppealChannelId)
            .SendMessageAsync("@here", embed: eb, components: comp)
            .ConfigureAwait(false);
        await FollowupAsync(
                "The admin team received your appeal, once the make a decision you will get a message",
                ephemeral: true
            )
            .ConfigureAwait(false);
    }
}
