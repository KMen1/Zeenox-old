﻿using System;
using System.ComponentModel;

namespace Zeenox.Enums;

[Flags]
public enum FilterType
{
    [Description("None")] None = 0,

    [Description("Bass Boost")] Bassboost = 1,

    [Description("Pop")] Pop = 2,

    [Description("Soft")] Soft = 4,

    [Description("Loud")] Treblebass = 8,

    [Description("Nightcore")] Nightcore = 16,

    [Description("8D")] Eightd = 32,

    [Description("Vaporwave")] Vaporwave = 64,

    [Description("Speed Up")] SpeedUp = 128,

    [Description("Speed Down")] SpeedDown = 256,

    [Description("Chipmunk")] Chipmunk = 512,

    [Description("Darth Vader")] Darthvader = 1024,

    [Description("Dance")] Dance = 2048,

    [Description("China")] China = 4096,

    [Description("Vibrate")] Vibrate = 8192,

    [Description("Vibrato")] Vibrato = 16384,

    [Description("Tremolo")] Tremolo = 32768,

    [Description("All")] All =
        Bassboost
        | Pop
        | Soft
        | Treblebass
        | Nightcore
        | Eightd
        | Vaporwave
        | SpeedUp
        | SpeedDown
        | Chipmunk
        | Darthvader
        | Dance
        | China
        | Vibrate
        | Vibrato
        | Tremolo
}