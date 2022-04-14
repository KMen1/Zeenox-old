using System.ComponentModel;

namespace KBot.Enums;

public enum TransactionType
{
    [Description("Unknown")]
    Unknown,
    [Description("Correction")]
    Correction,
    [Description("Gambling")]
    Gambling,
    [Description("Transfer Send")]
    TransferSend,
    [Description("Transfer Receive")]
    TransferReceive,
    [Description("Transfer Fee")]
    TransferFee,
    [Description("Daily Claim")]
    DailyClaim,
    [Description("Shop Purchase")]
    ShopPurchase,
}