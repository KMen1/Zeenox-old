using System;

namespace KBot.Enums;

[Flags]
public enum ShopItem
{
    None = 0,
    PlusOneLevel = 1,
    PlusTenLevel = 2,
    OwnRank = 4,
    OwnCategory = 8,
    OwnTextChannel = 16,
    OwnVoiceChannel = 32,
    All = PlusOneLevel | PlusTenLevel | OwnRank | OwnCategory | OwnTextChannel | OwnVoiceChannel
}