using System.Threading.Tasks;
using Discordance.Models;
using Discordance.Services;

namespace Discordance.Modules.Gambling.Games;

public interface IGame
{
    ulong UserId { get; }
    Task StartAsync();
    event AsyncEventHandler<GameEndEventArgs> GameEnded;
}