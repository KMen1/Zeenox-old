using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace KBot.Modules.Forms;

public class FormComponents : KBotModuleBase
{
    [ModalInteraction("appeal")]
    public async Task HandleAppealAsync(AppealSubmission submission)
    {
        await RespondAsync("Az illetékesek megkapták fellebezésed. Amint elfogadják/elutasítják értesítve leszel!", ephemeral: true).ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithAuthor(Context.User.Username, Context.User.GetAvatarUrl())
            .WithDescription("A fellebezés döntésre vár.")
            .WithTitle(submission.Title)
            .WithColor(Color.Orange)
            .AddField("Ki adta a büntetést?", submission.Moderator)
            .AddField("Milyen büntetést kaptál?", submission.AppealGiveReason)
            .AddField("Milyen okból kaptál büntetést?", submission.AdminReason)
            .AddField("Miért gondolod, hogy helytelenül kaptál büntetést?", submission.AppealReason)
            .Build();
        var comp = new ComponentBuilder()
            .WithButton("Elfogadás", $"appeal-accept:{Context.User.Id}", ButtonStyle.Success, new Emoji("✅"))
            .WithButton("Elutasítás", $"appeal-decline:{Context.User.Id}", ButtonStyle.Danger, new Emoji("❌"))
            .Build();
        await Context.Guild.GetTextChannel(941750604345270333).SendMessageAsync("@here",embed: eb, components: comp).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [ComponentInteraction("appeal-accept:*")]
    public async Task AcceptAppealAsync(string userId)
    {
        var embed = ((SocketMessageComponent) Context.Interaction).Message.Embeds.First().ToEmbedBuilder();
        var user = Context.Client.GetUser(Convert.ToUInt64(userId));
        if (user is not null)
        {
            var channel = await user.CreateDMChannelAsync().ConfigureAwait(false);
            var eb = new EmbedBuilder()
                .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
                .WithTitle("Elfogadott fellebezés")
                .WithDescription($"{Context.User.Mention} elfogadta a fellebezésedet.")
                .WithColor(Color.Green);
            foreach (var field in embed.Fields)
            {
                eb.AddField(field.Name, field.Value);
            }
            await channel.SendMessageAsync(embed: eb.Build()).ConfigureAwait(false);
        }

        embed.Color = Color.Green;
        embed.Description = $"A fellebbezés el lett fogadva {Context.User.Mention} által!";
        await ((SocketMessageComponent) Context.Interaction).Message.ModifyAsync(x =>
        {
            x.Embed = embed.Build();
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [ComponentInteraction("appeal-decline:*")]
    public async Task DeclineAppealAsync(string userId)
    {
        var embed = ((SocketMessageComponent) Context.Interaction).Message.Embeds.First().ToEmbedBuilder();
        var user = Context.Client.GetUser(Convert.ToUInt64(userId));
        if (user is not null)
        {
            var channel = await user.CreateDMChannelAsync().ConfigureAwait(false);
            var eb = new EmbedBuilder()
                .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
                .WithTitle("Elutasított fellebezés")
                .WithDescription($"{Context.User.Mention} elutasította a fellebezésedet.")
                .WithColor(Color.Red);
            foreach (var field in embed.Fields)
            {
                eb.AddField(field.Name, field.Value);
            }
            await channel.SendMessageAsync(embed: eb.Build()).ConfigureAwait(false);
        }

        embed.Color = Color.Red;
        embed.Description = $"A fellebbezés el lett utasítva {Context.User.Mention} által!";
        await ((SocketMessageComponent) Context.Interaction).Message.ModifyAsync(x =>
        {
            x.Embed = embed.Build();
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
    }
}