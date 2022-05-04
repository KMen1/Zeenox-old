using Discord.Interactions;

// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8618, MA0048

namespace KBot.Modules.Moderation;

public class AppealModal : IModal
{
    [ModalTextInput("appeal-reason")]
    public string AppealReason { get; set; }

    [ModalTextInput("appeal-acceptreason")]
    public string AcceptReason { get; set; }

    public string Title => "Appeal a warn";
}

public class ReasonModal : IModal
{
    [ModalTextInput("reason-input")]
    public string Reason { get; set; }

    public string Title => "Justify Decision";
}
