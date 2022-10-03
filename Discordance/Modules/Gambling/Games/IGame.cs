using System;
using System.Threading.Tasks;
using Discordance.Models;

namespace Discordance.Modules.Gambling.Games;

public interface IGame
{
    Task StartAsync();
    ulong UserId { get; }
    event EventHandler<GameEndEventArgs> GameEnded;
}
