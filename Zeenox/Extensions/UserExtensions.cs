using System;
using Zeenox.Models;

namespace Zeenox.Extensions;

public static class UserExtensions
{
    public static double GetWinRate(this User user)
    {
        return Math.Round(user.Wins / (double) user.GamesPlayed * 100, 2);
    }

    public static int GetMinimumBet(this User user)
    {
        return (int) Math.Round(Math.Pow(user.GambleLevel, 2.99996) + 185);
    }
}