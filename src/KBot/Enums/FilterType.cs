using System;

namespace KBot.Enums;

[Flags]
public enum FilterType
{
    None = 0,
    Bassboost = 1,
    Pop = 2,
    Soft = 4,
    Treblebass = 8,
    Nightcore = 16,
    Eightd = 32,
    Vaporwave = 64,
    Doubletime = 128,
    Slowmotion = 256,
    Chipmunk = 512,
    Darthvader = 1024,
    Dance = 2048,
    China = 4096,
    Vibrate = 8192,
    Vibrato = 16384,
    Tremolo = 32768,

    All = Bassboost | Pop | Soft | Treblebass | Nightcore | Eightd | Vaporwave | Doubletime | Slowmotion | Chipmunk |
          Darthvader | Dance | China | Vibrate | Vibrato | Tremolo
}