using System;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace KBot.Modules.Gambling;

public interface IGame
{
    Task StartAsync();
    event EventHandler<GameEndedArgs> GameEnded;
}

public class GameEndedArgs
{
    public GameEndedArgs(string gameId, SocketGuildUser user, int bet, int prize, string description, bool isWin)
    {
        Prize = prize;
        User = user;
        Bet = bet;
        Description = description;
        IsWin = isWin;
        GameId = gameId;
    }

    public string GameId { get; }
    public int Bet { get; }
    public SocketGuildUser User { get; }
    public int Prize { get; }
    public string Description { get; }
    public bool IsWin { get; }
}