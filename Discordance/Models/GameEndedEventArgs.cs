using System;
using Discordance.Enums;

namespace Discordance.Models;

public class GameEndEventArgs : EventArgs
{
    public GameEndEventArgs(ulong userId, int bet, int prize, GameResult result)
    {
        Prize = prize;
        UserId = userId;
        Bet = bet;
        Result = result;
    }

    public ulong UserId { get; }
    public int Bet { get; }
    public int Prize { get; }
    public GameResult Result { get; }
}