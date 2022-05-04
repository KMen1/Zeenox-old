using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace KBot.Modules.Moderation;

public class ModerationInteractions : SlashModuleBase
{
    [RequireUserPermission(GuildPermission.KickMembers)]
    [ComponentInteraction("appeal-accept:*:*:*")]
    public async Task AcceptAppealAsync(string warnId, ulong userId, ulong adminId)
    {
        if (Context.User.Id == adminId)
        {
            await RespondAsync(
                    embed: new EmbedBuilder()
                        .WithDescription("**You can not handle appeals that are against you!**")
                        .WithColor(Color.Red)
                        .Build(),
                    ephemeral: true
                )
                .ConfigureAwait(false);
            return;
        }
        var msgId = ((SocketMessageComponent)Context.Interaction).Message.Id;
        var modal = new ModalBuilder()
            .WithTitle("Justify Decision")
            .WithCustomId($"appeal-decision:{warnId}:{userId}:{msgId}:1")
            .AddTextInput(
                "Please justify your decision",
                "reason-input",
                TextInputStyle.Paragraph,
                "Accident..."
            )
            .Build();
        await RespondWithModalAsync(modal).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [ComponentInteraction("appeal-decline:*:*:*")]
    public async Task DeclineAppealAsync(string warnId, ulong userId, ulong adminId)
    {
        if (Context.User.Id == adminId)
        {
            await RespondAsync(
                    embed: new EmbedBuilder()
                        .WithDescription("**You can not handle appeals that are against you!**")
                        .WithColor(Color.Red)
                        .Build(),
                    ephemeral: true
                )
                .ConfigureAwait(false);
            return;
        }
        var msgId = ((SocketMessageComponent)Context.Interaction).Message.Id;
        var modal = new ModalBuilder()
            .WithTitle("Justify Decision")
            .WithCustomId($"appeal-decision:{warnId}:{userId}:{msgId}:0")
            .AddTextInput(
                "Please justify your decision:",
                "reason-input",
                TextInputStyle.Paragraph,
                "Accident..."
            )
            .Build();
        await RespondWithModalAsync(modal).ConfigureAwait(false);
    }

    [ModalInteraction("appeal-decision:*:*:*:*")]
    public async Task AppealDecisionAsync(
        string warnId,
        ulong userId,
        ulong messageId,
        int decision,
        ReasonModal modal
    )
    {
        await DeferAsync(true).ConfigureAwait(false);
        var msg = (IUserMessage)await Context.Channel
            .GetMessageAsync(messageId)
            .ConfigureAwait(false);
        var appealerUser = Context.Client.GetUser(userId);
        var appealEmbed = msg.Embeds.First().ToEmbedBuilder();

        var userEb = new EmbedBuilder()
            .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
            .WithTitle(decision == 1 ? "Appeal Accepted" : "Appeal Declined")
            .WithColor(decision == 1 ? Color.Green : Color.Red)
            .WithDescription(
                decision == 1
                  ? $"{Context.User.Mention} accepted your appeal with reason: `{modal.Reason}`"
                  : $"{Context.User.Mention} denied your appeal with reason: `{modal.Reason}`"
            );

        appealEmbed.WithColor(decision == 1 ? Color.Green : Color.Red);
        appealEmbed.Fields[0].Value =
            decision == 1
                ? $"**Accepted by:** {Context.User.Mention}\n**Reason:** {modal.Reason}"
                : $"**Denied by:** {Context.User.Mention}\n**Reason**: {modal.Reason}";

        if (decision == 1)
        {
            await Mongo.RemoveWarnAsync(warnId).ConfigureAwait(false);
        }

        var channel = await appealerUser.CreateDMChannelAsync().ConfigureAwait(false);
        try
        {
            await channel.SendMessageAsync(embed: userEb.Build()).ConfigureAwait(false);
        }
        catch
        {
            // ignored
        }

        await msg.ModifyAsync(
                x =>
                {
                    x.Embed = appealEmbed.Build();
                    x.Components = new ComponentBuilder().Build();
                }
            )
            .ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithDescription("**Success**")
                    .WithColor(Color.Red)
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }
}
