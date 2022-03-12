using System.Threading.Tasks;
using CloudinaryDotNet;
using Discord;
using Discord.WebSocket;
using KBot.Modules.Gambling.BlackJack;
using KBot.Modules.Gambling.Crash;
using KBot.Modules.Gambling.HighLow;
using KBot.Modules.Gambling.Mines;
using KBot.Modules.Gambling.Objects;
using KBot.Services;

namespace KBot.Modules.Gambling;

public class GamblingService
{
    private readonly Cloudinary _cloudinary;
    private readonly DatabaseService Database;

    private readonly BlackJackService BlackJack;
    private readonly HighLowService HighLow;
    private readonly CrashService Crash;
    private readonly MinesService Mines;

    public GamblingService(Cloudinary cloudinary, DatabaseService databaseService)
    {
        _cloudinary = cloudinary;
        Database = databaseService;
        BlackJack = new BlackJackService(Database, _cloudinary);
        Crash = new CrashService(Database);
        HighLow = new HighLowService(Database, _cloudinary);
        Mines = new MinesService(Database);
    }

    public BlackJackGame GetBlackJackGame(string id)
    {
        return BlackJack.GetGame(id);
    }
    public HighLowGame GetHighLowGame(string id)
    {
        return HighLow.GetGame(id);
    }
    public MinesGame GetMinesGame(string id)
    {
        return Mines.GetGame(id);
    }
    public BlackJackGame CreateBlackJackGame(SocketUser user, IUserMessage message, int stake)
    {
        return BlackJack.CreateGame(Generators.GenerateID(), user, message, stake);
    }
    public HighLowGame CreateHighLowGame(SocketUser user, IUserMessage message, int stake)
    {
        return HighLow.CreateGame(user, message, stake);
    }
    public Task StartCrashGameAsync(SocketUser user, IUserMessage message, int bet)
    {
        return Crash.StartGameAsync(Generators.GenerateID(), user, message, bet);
    }
    public MinesGame CreateMinesGame(SocketUser user, IUserMessage message, int bet, int mines)
    {
        return Mines.CreateGame(user, message, bet, 5, mines);
    }
    public Task StopCrashGameAsync(string id)
    {
        return Crash.StopGameAsync(id);
    }
}