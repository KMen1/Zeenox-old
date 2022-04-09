using System;
using System.Threading.Tasks;

namespace KBot.Modules.Gambling;

public interface IGamblingGame
{
    Task StartAsync();
    event EventHandler<GameEndedEventArgs> GameEnded;
}

public class GameEndedEventArgs
{
    public GameEndedEventArgs(string gameId, int prize, string description, bool isWin)
    {
        Prize = prize;
        Description = description;
        IsWin = isWin;
        GameId = gameId;
    }
    public string GameId { get; }
    public int Prize { get; }
    public string Description { get; }
    public bool IsWin { get; }
}