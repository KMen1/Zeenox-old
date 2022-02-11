using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.Forms;

public class FormComponents : KBotModuleBase
{
    [ModalInteraction("appeal")]
    public async Task HandleApplyForAdminAsync(AppealSubmission submission)
    {
        await RespondAsync("Az illetékesek megkapták fellebezésed. Amint elfogadják/elutasítják értesítve leszel!", ephemeral: true).ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithAuthor(Context.User.Username, Context.User.GetAvatarUrl())
            .WithDescription(Context.User.Mention + " új fellebezést küldött")
            .WithTitle(submission.Title)
            .AddField("Ki adta a büntetést?", submission.Moderator)
            .AddField("Milyen büntetést kaptál?", submission.AppealGiveReason)
            .AddField("Milyen okból kaptál büntetést?", submission.AdminReason)
            .AddField("Miért gondolod, hogy helytelenül kaptál büntetést?", submission.AppealReason)
            .Build();
        var comp = new ComponentBuilder()
            .WithButton("Elfogadás", "appeal-accept", ButtonStyle.Success, new Emoji("✅"))
            .WithButton("Elutasítás", "appeal-decline", ButtonStyle.Danger, new Emoji("❌"))
            .Build();
        await Context.Guild.GetTextChannel(925317005433769984).SendMessageAsync("@here",embed: eb).ConfigureAwait(false);
    }
}