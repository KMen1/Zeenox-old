using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace KBot.Modules.Forms;

[Group("form", "Kitölthető űrlapok")]
public class FormCommands : KBotModuleBase
{
    [SlashCommand("appeal", "Warn, Timeout, Mute fellebbezés")]
    public async Task AppealAsync(
        [Summary("Admin", "Az admin aki adta a büntetést")] SocketUser admin,
        [Summary("WarnID", "Warn fellebezése esetén a warn ID-je")] int warnId = 0)
    {
        if (!Context.Guild.GetUser(admin.Id).GuildPermissions.KickMembers)
        {
            await RespondAsync("A megadott felhasználó nem admin! Kérlek, próbáld újra.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        var modal = new ModalBuilder()
            .WithTitle("Büntetés fellebezés")
            .AddTextInput("Milyen büntetést kaptál?", "appeal-punishtype", TextInputStyle.Short,
                "pl. Warn/mute/timeout", required: true)
            .AddTextInput("Miért gondolod, hogy helytelenül kaptad?", "appeal-reason", TextInputStyle.Paragraph,
                "pl. Ideges volt az admin, stb.", required: true);

        if (warnId == 0)
        {
            modal.WithCustomId($"appeal:{admin.Id}:0")
                .AddTextInput("Miért kaptál büntetést?", "appeal-punishreason", TextInputStyle.Paragraph,
                    "pl. Ha megadtál warn ID-t ezt nem kell kitöltened", required: true);
            await RespondWithModalAsync(modal.Build()).ConfigureAwait(false);
            return;
        }
        var warns = (await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false)).Warns;
        if (warns.Count < warnId)
        {
            await RespondAsync("Nincs ilyen ID-jű warnod!", ephemeral: true).ConfigureAwait(false);
            return;
        }

        modal.WithCustomId($"appeal:{admin.Id}:{warnId}")
            .AddTextInput("Miért kaptál büntetést?", "appeal-punishreason", TextInputStyle.Paragraph,
                "pl. Halállal való fenyegetés", required: false)
            .Build();

        await RespondWithModalAsync(modal.Build()).ConfigureAwait(false);
    }
}