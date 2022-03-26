using System;
using System.Threading.Tasks;
using CloudinaryDotNet;
using Discord;
using Discord.WebSocket;
using KBot.Modules.Gambling.BlackJack;
using KBot.Modules.Gambling.Crash;
using KBot.Modules.Gambling.HighLow;
using KBot.Modules.Gambling.Mines;
using KBot.Modules.Gambling.Objects;
using KBot.Modules.Gambling.Towers;
using KBot.Services;

namespace KBot.Modules.Gambling;

public class GamblingService
{
    private readonly BlackJackService BlackJack;
    private readonly HighLowService HighLow;
    private readonly CrashService Crash;
    private readonly MinesService Mines;
    private readonly TowersService Towers;

    public GamblingService(Cloudinary cloudinary, DatabaseService database)
    {
        BlackJack = new BlackJackService(database, cloudinary);
        Crash = new CrashService(database);
        HighLow = new HighLowService(database, cloudinary);
        Mines = new MinesService();
        Towers = new TowersService();
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
        return BlackJack.CreateGame(Guid.NewGuid().ToString().Split("-")[0], user, message, stake);
    }
    public HighLowGame CreateHighLowGame(SocketUser user, IUserMessage message, int stake)
    {
        return HighLow.CreateGame(user, message, stake);
    }

    public MinesGame CreateMinesGame(SocketUser user, IUserMessage message, int bet, int mines)
    {
        return Mines.CreateGame(user, message, bet, 5, mines);
    }
    public Task StopCrashGameAsync(string id)
    {
        return Crash.StopGameAsync(id);
    }

    public CrashGame CreateCrashGame(SocketUser user, IUserMessage msg, int bet)
    {
        return Crash.CreateGame(Guid.NewGuid().ToString().Split("-")[0], user, msg, bet);
    }

    public CrashGame GetCrashGame(string id)
    {
        return Crash.GetGame(id);
    }

    public TowersGame CreateTowersGame(SocketUser contextUser, IUserMessage msg, int bet, Difficulty diff)
    {
        return Towers.CreateGame(contextUser, msg, bet, diff);
    }
    
    public TowersGame GetTowersGame(string id)
    {
        return Towers.GetGame(id);
    }
}