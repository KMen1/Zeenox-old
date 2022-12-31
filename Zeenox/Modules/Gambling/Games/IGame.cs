using System.Threading.Tasks;
using Zeenox.Models.Games;
using Zeenox.Services;

namespace Zeenox.Modules.Gambling.Games;

public interface IGame
{
    ulong UserId { get; }
    Task StartAsync();
    event AsyncEventHandler<GameEndEventArgs> GameEnded;
}