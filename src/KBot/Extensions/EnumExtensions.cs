using System;
using System.ComponentModel;
using Discord;
using KBot.Enums;

namespace KBot.Extensions;

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value);
        if (name == null) return null;
        var field = type.GetField(name);
        if (field == null) return null;
        if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr)
        {
            return attr.Description;
        }
        return null;
    }
    
    public static string GetGradeEmoji(this Grade grade)
    {
        return grade switch
        {
            Grade.N => "<:osuF:936588252763271168>",
            Grade.F => "<:osuF:936588252763271168>",
            Grade.D => "<:osuD:936588252884910130>",
            Grade.C => "<:osuC:936588253031723078>",
            Grade.B => "<:osuB:936588252830380042>",
            Grade.A => "<:osuA:936588252754882570>",
            Grade.S => "<:osuS:936588252872318996>",
            Grade.SH => "<:osuSH:936588252834574336>",
            Grade.X => "<:osuX:936588252402573333>",
            Grade.XH => "<:osuXH:936588252822007818>",
            _ => "<:osuF:936588252763271168>"
        };
    }
    public static Color GetGradeColor(this Grade grade)
    {
        return grade switch
        {
            Grade.N => Color.Default,
            Grade.F => new Color(109, 73, 38),
            Grade.D => Color.Red,
            Grade.C => Color.Purple,
            Grade.B => Color.Blue,
            Grade.A => Color.Green,
            Grade.S => Color.Gold,
            Grade.SH => Color.LightGrey,
            Grade.X => Color.Gold,
            Grade.XH => Color.LightGrey,
            _ => Color.Default
        };
    }
}