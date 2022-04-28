using System.ComponentModel;

namespace KBot.Enums;

public enum TransactionType
{
    [Description("Unknown")] Unknown,
    [Description("Correction")] Correction,
    [Description("Gambling")] Gambling,
    [Description("Blackjack")] Blackjack,
    [Description("Crash")] Crash,
    [Description("Highlow")] Highlow,
    [Description("Mines")] Mines,
    [Description("Towers")] Towers,
    [Description("Transfer")] Transfer,
    [Description("Daily Claim")] DailyClaim,
    [Description("Shop Purchase")] ShopPurchase,
    [Description("Extra Levels")] LevelPurchase,
    [Description("Category")] CategoryPurchase,
    [Description("Role")] RolePurchase,
    [Description("Text Channel")] TextPurchase,
    [Description("Voice Channel")] VoicePurchase
}