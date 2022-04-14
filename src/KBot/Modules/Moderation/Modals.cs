using Discord.Interactions;

namespace KBot.Modules.Moderation;

public class AppealModal : IModal
{
    public string Title => "Warn appeal";

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
    public string Title => "Justify Decision";
    
    [ModalTextInput("reason-input")]
    public string Reason { get; set; }
}