using System.Threading.Tasks;
using CloudinaryDotNet;
using Discord;
using Discord.WebSocket;
using KBot.Models;
using KBot.Modules.Gambling.BlackJack;
using KBot.Modules.Gambling.Crash;
using KBot.Modules.Gambling.HighLow;
using KBot.Services;

namespace KBot.Modules.Gambling;

public class GamblingService
{
    private readonly Cloudinary _cloudinary;
    private readonly BlackJackService BlackJack = new();
    private readonly HighLowService HighLow = new();
    private readonly CrashService Crash = new();
    private readonly DatabaseService Database;
    public GamblingService(Cloudinary cloudinary, DatabaseService databaseService)
    {
        _cloudinary = cloudinary;
        Database = databaseService;
    }

    public BlackJackGame GetBlackJackGame(string id)
    {
        return BlackJack.GetGame(id);
    }

    public HighLowGame GetHighLowGame(string id)
    {
        return HighLow.GetGame(id);
    }

    public BlackJackGame CreateBlackJackGame(SocketUser user, int stake)
    {
        return BlackJack.CreateGame(user, stake, _cloudinary);
    }

    public HighLowGame CreateHighLowGame(SocketUser user, int stake)
    {
        return HighLow.CreateGame(user, stake, _cloudinary);
    }

    public Task StartCrashGameAsync(string id, SocketUser user, IUserMessage message, int bet, User dbUser)
    {
        return Crash.StartGameAsync(id, user, message, bet, dbUser, Database);
    }
    public void RemoveBlackJackGame(string id)
    {
        BlackJack.RemoveGame(id);
    }

    public void RemoveBlackJackGame(BlackJackGame game)
    {
        BlackJack.RemoveGame(game);
    }

    public void RemoveHighLowGame(string id)
    {
        HighLow.RemoveGame(id);
    }

    public Task StopCrashGameAsync(string id)
    {
        return Crash.StopGameAsync(id);
    }
}