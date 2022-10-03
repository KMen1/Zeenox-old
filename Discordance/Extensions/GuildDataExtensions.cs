using System;
using Discordance.Models;

namespace Discordance.Extensions;

public static class GuildDataExtensions
{
    public static int GetRequiredXp(this GuildData data)
    {
        return Convert.ToInt32(Math.Pow(data.Level * 4, 2));
    }
}