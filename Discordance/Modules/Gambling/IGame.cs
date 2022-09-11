using System;
using System.Threading.Tasks;
using Discord;
using Discordance.Models;

namespace Discordance.Modules.Gambling;

public interface IGame
{
    Task StartAsync();
    ulong UserId { get; }
    event EventHandler<GameEndEventArgs> GameEnded;
}
