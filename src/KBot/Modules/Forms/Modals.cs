using Discord.Interactions;

namespace KBot.Modules.Forms;

public class AppealModal : IModal
{
    public string Title => "Büntetés fellebezés";

    [ModalTextInput("appeal-moderator")]
    public string Moderator { get; set; }

    [ModalTextInput("appeal-punishtype")]
    public string PunishType { get; set; }

    [ModalTextInput("appeal-punishreason")]
    public string PunishReason { get; set; }

    [ModalTextInput("appeal-reason")]
    public string AppealReason { get; set; }
}

public class ReasonModal : IModal
{
    public string Title => "Döntés indoklása";
    
    [ModalTextInput("reason-input")]
    public string Reason { get; set; }
}