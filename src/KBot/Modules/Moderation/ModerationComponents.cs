using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Models.User;

namespace KBot.Modules.Moderation;

public class ModerationComponents : SlashModuleBase
{
    [ModalInteraction("appeal:*:*")]
    public async Task HandleAppealAsync(ulong adminId, int warnId, AppealModal submission)
    {
        var admin = Context.Guild.GetUser(adminId);
        Warn warn = null;
        if (warnId != 0)
            warn = (await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false)).Warns[warnId - 1];

        var eb = new EmbedBuilder()
            .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
            .WithTitle("Appeal waiting for review")
            .WithDescription($"Requested by: {Context.User.Mention}")
            .WithColor(Color.Orange)
            .AddField("Who gave the punishment?", $"{admin.Mention}")
            .AddField("What kind of punishment?", submission.PunishType)
            .AddField("Reason for punishment", warn is null ? submission.PunishReason : warn.Reason)
            .AddField("Reason for appeal", submission.AppealReason)
            .Build();
        var comp = new ComponentBuilder()
            .WithButton("Accept", $"appeal-accept:{Context.User.Id}:{admin.Id}", ButtonStyle.Success, new Emoji("✅"))
            .WithButton("Deny", $"appeal-decline:{Context.User.Id}:{admin.Id}", ButtonStyle.Danger, new Emoji("❌"))
            .Build();
        await Context.Guild.GetTextChannel(941750604345270333).SendMessageAsync("@here", embed: eb, components: comp)
            .ConfigureAwait(false);
        await RespondAsync("The mod team received your appeal, once the make a decision you will get a message",
            ephemeral: true).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [ComponentInteraction("appeal-accept:*:*")]
    public async Task AcceptAppealAsync(ulong userId, ulong adminId)
    {
        if (Context.User.Id == adminId)
            await RespondAsync("You cannot accept appeals against you...", ephemeral: true)
                .ConfigureAwait(false);
        var msgId = ((SocketMessageComponent) Context.Interaction).Message.Id;
        var modal = new ModalBuilder()
            .WithTitle("Justify Decision")
            .WithCustomId($"appeal-decision:{userId}:{adminId}:{msgId}:1")
            .AddTextInput("Please justify your decision", "reason-input", TextInputStyle.Paragraph,
                "Accident...")
            .Build();
        await RespondWithModalAsync(modal).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [ComponentInteraction("appeal-decline:*:*")]
    public async Task DeclineAppealAsync(ulong userId, ulong adminId)
    {
        if (Context.User.Id == adminId)
            await RespondAsync("You cannot deny appeals against you...", ephemeral: true)
                .ConfigureAwait(false);
        var msgId = ((SocketMessageComponent) Context.Interaction).Message.Id;
        var modal = new ModalBuilder()
            .WithTitle("Justify Decision")
            .WithCustomId($"appeal-decision:{userId}:{adminId}:{msgId}:0")
            .AddTextInput("Please justify your decision:", "reason-input", TextInputStyle.Paragraph,
                "Accident...")
            .Build();
        await RespondWithModalAsync(modal).ConfigureAwait(false);
    }

    [ModalInteraction("appeal-decision:*:*:*:*")]
    public async Task AppealDecisionAsync(ulong userId, ulong adminId, ulong messageId, string decision,
        ReasonModal modal)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var msg =
            (IUserMessage) await Context.Channel.GetMessageAsync(messageId).ConfigureAwait(false);
        var admin = Context.Guild.GetUser(Convert.ToUInt64(adminId));
        var user = Context.Guild.GetUser(Convert.ToUInt64(userId));
        var embed = msg.Embeds.First().ToEmbedBuilder();

        var userEb = new EmbedBuilder();
        if (decision == "1")
        {
            userEb.WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
                .WithTitle("Appeal Accepted")
                .WithDescription($"{Context.User.Mention} accepted you appeal with reason: `{modal.Reason}`")
                .WithColor(Color.Green);
            foreach (var field in embed.Fields) userEb.AddField(field.Name, field.Value);
            embed.Color = Color.Green;
            embed.Title = "Appeal Accepted";
            embed.Description += $"\nAccepted by: {Context.User.Mention}\nReason: `{modal.Reason}`";
        }
        else
        {
            userEb.WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
                .WithTitle("Appeal Denied")
                .WithDescription($"{Context.User.Mention} denied you appeal with reason: `{modal.Reason}`")
                .WithColor(Color.Green);
            foreach (var field in embed.Fields) userEb.AddField(field.Name, field.Value);
            embed.Color = Color.Green;
            embed.Title = "Appeal Denied";
            embed.Description += $"\nDenied by: {Context.User.Mention}\nReason: `{modal.Reason}`";
        }

        if (user is not null)
        {
            var channel = await user.CreateDMChannelAsync().ConfigureAwait(false);
            await channel.SendMessageAsync(embed: userEb.Build()).ConfigureAwait(false);
        }

        await msg.ModifyAsync(x =>
        {
            x.Embed = embed.Build();
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);

        await FollowupAsync("Success!", ephemeral: true).ConfigureAwait(false);
    }
}