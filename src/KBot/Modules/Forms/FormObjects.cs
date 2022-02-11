using Discord.Interactions;

namespace KBot.Modules.Forms;

public class AppealSubmission : IModal
{
    public string Title => "Büntetés fellebezés";

    [ModalTextInput("appeal-moderator")]
    public string Moderator { get; set; }

    [ModalTextInput("appeal-punishtype")]
    public string AdminReason { get; set; }

    [ModalTextInput("appeal-punishreason")]
    public string AppealGiveReason { get; set; }

    [ModalTextInput("appeal-reason")]
    public string AppealReason { get; set; }
}