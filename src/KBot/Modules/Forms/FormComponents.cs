using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Models;

namespace KBot.Modules.Forms;

public class FormComponents : KBotModuleBase
{
    [ModalInteraction("appeal:*:*")]
    public async Task HandleAppealAsync(string adminId, string warnId, AppealModal submission)
    {
        var admin = Context.Guild.GetUser(ulong.Parse(adminId));
        Warn warn = null;
        if (warnId != "0")
        {
            warn = (await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false)).Warns[int.Parse(warnId) - 1];
        }

        var eb = new EmbedBuilder()
            .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
            .WithTitle("A fellebezés döntésre vár")
            .WithDescription($"Kérelmezte: {Context.User.Mention}")
            .WithColor(Color.Orange)
            .AddField("Ki adta a büntetést?", $"{admin.Mention}")
            .AddField("Milyen büntetést kaptál?", submission.PunishType)
            .AddField("Milyen okból kaptál büntetést?", warn is null ? submission.PunishReason : warn.Reason)
            .AddField("Miért gondolod, hogy helytelenül kaptál büntetést?", submission.AppealReason)
            .Build();
        var comp = new ComponentBuilder()
            .WithButton("Elfogadás", $"appeal-accept:{Context.User.Id}:{admin.Id}", ButtonStyle.Success, new Emoji("✅"))
            .WithButton("Elutasítás", $"appeal-decline:{Context.User.Id}:{admin.Id}", ButtonStyle.Danger, new Emoji("❌"))
            .Build();
        await Context.Guild.GetTextChannel(941750604345270333).SendMessageAsync("@here",embed: eb, components: comp).ConfigureAwait(false);
        await RespondAsync("Az illetékesek megkapták fellebezésed. Amint elfogadják/elutasítják értesítve leszel!", ephemeral: true).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [ComponentInteraction("appeal-accept:*:*")]
    public async Task AcceptAppealAsync(string userId, string adminId)
    {
        if (Context.User.Id == ulong.Parse(adminId))
        {
            await RespondAsync("Az ellened szóló fellebezéseket nem tudod elfogadni...", ephemeral: true)
                .ConfigureAwait(false);
        }
        var msgId = ((SocketMessageComponent) Context.Interaction).Message.Id;
        var modal = new ModalBuilder()
            .WithTitle("Döntés indoklása")
            .WithCustomId($"appeal-decision:{userId}:{adminId}:{msgId}:1")
            .AddTextInput("Kérlek indokold meg a döntésed:", "reason-input", TextInputStyle.Paragraph,
                "Véletlen adtam, nem fenyegetett halállal...")
            .Build();
        await RespondWithModalAsync(modal).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [ComponentInteraction("appeal-decline:*:*")]
    public async Task DeclineAppealAsync(string userId, string adminId)
    {
        if (Context.User.Id == ulong.Parse(adminId))
        {
            await RespondAsync("Az ellened szóló fellebezéseket nem tudod elutasítani...", ephemeral: true)
                .ConfigureAwait(false);
        }
        var msgId = ((SocketMessageComponent) Context.Interaction).Message.Id;
        var modal = new ModalBuilder()
            .WithTitle("Döntés indoklása")
            .WithCustomId($"appeal-decision:{userId}:{adminId}:{msgId}:0")
            .AddTextInput("Kérlek indokold meg a döntésed:", "reason-input", TextInputStyle.Paragraph,
                "Véletlen adtam, nem fenyegetett halállal...")
            .Build();
        await RespondWithModalAsync(modal).ConfigureAwait(false);
    }

    [ModalInteraction("appeal-decision:*:*:*:*")]
    public async Task AppealDecisionAsync(string userId, string adminId, string messageId, string decision, ReasonModal modal)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var msg =
            (IUserMessage)await Context.Channel.GetMessageAsync(ulong.Parse(messageId)).ConfigureAwait(false);
        var admin = Context.Guild.GetUser(Convert.ToUInt64(adminId));
        var user = Context.Guild.GetUser(Convert.ToUInt64(userId));
        var embed = msg.Embeds.First().ToEmbedBuilder();

        var userEb = new EmbedBuilder();
        if (decision == "1")
        {
            userEb.WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
                .WithTitle("Elfogadott fellebezés")
                .WithDescription($"{Context.User.Mention} elfogadta a fellebezésedet.\nIndok: `{modal.Reason}`")
                .WithColor(Color.Green);
            foreach (var field in embed.Fields)
            {
                userEb.AddField(field.Name, field.Value);
            }
            embed.Color = Color.Green;
            embed.Title = "Elfogadott fellebbezés";
            embed.Description += $"\nElfogadta: {Context.User.Mention}\nIndok: `{modal.Reason}`";
        }
        else
        {
            userEb.WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
                .WithTitle("Elutasított fellebezés")
                .WithDescription($"{Context.User.Mention} elutasította a fellebezésedet.\nIndok: `{modal.Reason}`")
                .WithColor(Color.Green);
            foreach (var field in embed.Fields)
            {
                userEb.AddField(field.Name, field.Value);
            }
            embed.Color = Color.Green;
            embed.Title = "Elutasított fellebbezés";
            embed.Description += $"\nElutasította: {Context.User.Mention}\nIndok: `{modal.Reason}`";
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
        
        await FollowupAsync("Sikeres döntés!", ephemeral: true).ConfigureAwait(false);
    }
}