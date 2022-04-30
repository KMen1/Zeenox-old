using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Extensions;

namespace KBot.Modules.Gambling.Crash.Game;

public sealed class CrashGame : IGame
{
    public CrashGame(
        SocketGuildUser user,
        IUserMessage message,
        int bet,
        double crashPoint)
    {
        Id = Guid.NewGuid().ToShortId();
        User = user;
        Message = message;
        Bet = bet;
        CrashPoint = crashPoint;
        TokenSource = new CancellationTokenSource();
        StoppingToken = TokenSource.Token;
    }

    public string Id { get; }
    public SocketGuildUser User { get; }
    private IUserMessage Message { get; }
    public int Bet { get; }
    private double CrashPoint { get; }
    public double Multiplier { get; private set; }
    public int Profit => (int) (Bet * Multiplier - Bet);
    private CancellationTokenSource TokenSource { get; }
    private CancellationToken StoppingToken { get; }
    public event EventHandler<GameEndedEventArgs>? GameEnded;

    public async Task StartAsync()
    {
        await Message.ModifyAsync(x =>
        {
            x.Content = string.Empty;
            x.Embed = new CrashEmbedBuilder(this).Build();
            x.Components = new ComponentBuilder()
                .WithButton(" ", $"crash:{Id}", ButtonStyle.Danger, new Emoji("🛑"))
                .Build();
        }).ConfigureAwait(false);
        Multiplier = 1.00;
        while (!StoppingToken.IsCancellationRequested)
        {
            Multiplier += 0.10;
            await Message.ModifyAsync(x => x.Embed = new CrashEmbedBuilder(this).Build()).ConfigureAwait(false);

            if (Multiplier >= CrashPoint)
            {
                await Message.ModifyAsync(x =>
                {
                    x.Embed = new CrashEmbedBuilder(this,
                        $"**Crashed at:** {CrashPoint:0.00}x\n**Result:** You lost **{Bet.ToString("N0", CultureInfo.InvariantCulture)}** credits")
                        .WithColor(Color.Red)
                        .Build();
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                OnGameEnded(new GameEndedEventArgs(Id, User, Bet, 0, "Crash: LOSE", false));
                break;
            }

            await Task.Delay(2000).ConfigureAwait(false);
        }
    }

    public async Task StopAsync()
    {
        TokenSource.Cancel();
        await Message.ModifyAsync(x =>
        {
            x.Embed = new CrashEmbedBuilder(this, $"**Stopped at:** {Multiplier:0.00}x\n" +
                                                  $"**Crashpoint:** {CrashPoint:0.00}x\n" +
                                                  $"**Result:** You win **{Profit.ToString("N0", CultureInfo.InvariantCulture)}** credits")
                .WithColor(Color.Green)
                .Build();
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
        OnGameEnded(new GameEndedEventArgs(Id, User, Bet, Profit, $"Crash: {Multiplier:0.0}x", false));
    }

    private void OnGameEnded(GameEndedEventArgs e)
    {
        GameEnded?.Invoke(this, e);
    }
}