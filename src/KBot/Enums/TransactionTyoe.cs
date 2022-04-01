using System.ComponentModel;

namespace KBot.Enums;

public enum TransactionType
{
    [Description("Ismeretlen")]
    Unknown,
    [Description("Korrekció")]
    Correction,
    [Description("Szerencsejáték")]
    Gambling,
    [Description("Utalás küldés")]
    TransferSend,
    [Description("Utalás fogadás")]
    TransferReceive,
    [Description("Utalás díj")]
    TransferFee,
    [Description("Napi begyűjtés")]
    DailyClaim,
    [Description("Vásárlás")]
    ShopPurchase,
}