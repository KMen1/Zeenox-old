using System;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace KBot.Modules.Gambling;

public interface IGame
{
    Task StartAsync();
    string Id { get; }
    SocketGuildUser User { get; }
    event EventHandler<GameEndedEventArgs> GameEnded;
}

public class GameEndedEventArgs : EventArgs
{
    public GameEndedEventArgs(
        string gameId,
        SocketGuildUser user,
        int bet,
        int prize,
        string description,
        bool isWin
    )
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
