using Discord.Interactions;
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8618, MA0048

namespace KBot.Modules.Moderation;

public class AppealModal : IModal
{
    [ModalTextInput("appeal-moderator")] public string Moderator { get; set; }

    [ModalTextInput("appeal-punishtype")] public string PunishType { get; set; }

    [ModalTextInput("appeal-punishreason")]
    public string PunishReason { get; set; }

    [ModalTextInput("appeal-reason")] public string AppealReason { get; set; }

    public string Title => "Warn appeal";
}

public class ReasonModal : IModal
{
    [ModalTextInput("reason-input")] public string Reason { get; set; }

    public string Title => "Justify Decision";
}