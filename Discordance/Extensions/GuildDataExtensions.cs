using System;
using Discordance.Models;

namespace Discordance.Extensions;

public static class GuildDataExtensions
{
    public static int GetRequiredXp(this GuildData data) =>
        Convert.ToInt32(Math.Pow(data.Level * 4, 2));
}
