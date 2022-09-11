using System;
using System.Linq;
using Discord.WebSocket;
using Discordance.Models;

namespace Discordance.Extensions;

public static class UserExtensions
{
    public static double GetWinRate(this User user) =>
        Math.Round(user.Wins / (double)user.GamesPlayed * 100, 2);

    public static int GetMinimumBet(this User user) =>
        (int)Math.Round(Math.Pow(user.GambleLevel, 2.99996) + 185);
}
