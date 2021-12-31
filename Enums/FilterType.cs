using System;
using Discord.Interactions;

namespace KBot.Enums;

[Flags]
public enum FilterType
{
    [Hide] None = 0,
    BassBoost = 1 << 0,
    NightCore = 1 << 1,
    EightD = 1 << 2,
    VaporWave = 1 << 3,
    [Hide] All = BassBoost | NightCore | EightD | VaporWave
}