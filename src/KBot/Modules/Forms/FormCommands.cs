using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.Forms;

[Group("form", "Kitölthető űrlapok")]
public class FormCommands : KBotModuleBase
{
    [SlashCommand("appeal", "Warn/Timeout/Ban fellebbezés")]
    public Task ApplyForAdminAsync()
    {
        var modal = new ModalBuilder()
            .WithTitle("Büntetés fellebezés")
            .WithCustomId("appeal")
            .AddTextInput("Ki adta a büntetést?", "appeal-moderator", TextInputStyle.Short, 
               "pl. KMen#1290", required: true)
            .AddTextInput("Milyen büntetést kaptál?", "appeal-punishtype", TextInputStyle.Short, 
                "pl. Warn/mute/timeout", required: true)
            .AddTextInput("Milyen okból kaptál büntetést?", "appeal-punishreason", TextInputStyle.Paragraph,
                "pl. Szabályzat hanyas pont, stb.", required: true)
            .AddTextInput("Miért gondolod, hogy helytelenül kaptad?", "appeal-reason", TextInputStyle.Paragraph,
                "pl. Ideges volt az admin, stb.", required: true)
            .Build();
        return RespondWithModalAsync(modal);
    }
}