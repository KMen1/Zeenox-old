using System;
using System.Threading.Tasks;
using Discordance.Models;

namespace Discordance.Modules.Gambling.Games;

public interface IGame
{
    ulong UserId { get; }
    Task StartAsync();
    event EventHandler<GameEndEventArgs> GameEnded;
}