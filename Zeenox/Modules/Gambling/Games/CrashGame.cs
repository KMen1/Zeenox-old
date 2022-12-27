using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Zeenox.Enums;
using Zeenox.Models;
using Zeenox.Services;

namespace Zeenox.Modules.Gambling.Games;

public sealed class Crash : IGame
{
    public Crash(ulong userId, IUserMessage message, int bet, double crashPoint)
    {
        UserId = userId;
        Message = message;
        Bet = bet;
        CrashPoint = crashPoint;
        TokenSource = new CancellationTokenSource();
        StoppingToken = TokenSource.Token;
    }

    private IUserMessage Message { get; }
    public int Bet { get; }
    private double CrashPoint { get; }
    public double Multiplier { get; private set; }
    public int Profit => (int) (Bet * Multiplier - Bet);
    private CancellationTokenSource TokenSource { get; }
    private CancellationToken StoppingToken { get; }

    public ulong UserId { get; }
    public event AsyncEventHandler<GameEndEventArgs> GameEnded = null!;

    public async Task StartAsync()
    {
        await Message
            .ModifyAsync(
                x =>
                {
                    x.Content = string.Empty;
                    x.Embed = new CrashEmbedBuilder(this).Build();
                    x.Components = new ComponentBuilder()
                        .WithButton(" ", "crash", ButtonStyle.Danger, new Emoji("🛑"))
                        .Build();
                }
            )
            .ConfigureAwait(false);
        Multiplier = 1.00;
        while (!StoppingToken.IsCancellationRequested)
        {
            Multiplier += 0.10;
            await Message
                .ModifyAsync(x => x.Embed = new CrashEmbedBuilder(this).Build())
                .ConfigureAwait(false);

            if (Multiplier >= CrashPoint)
            {
                await Message
                    .ModifyAsync(
                        x =>
                        {
                            x.Embed = new CrashEmbedBuilder(
                                    this,
                                    $"**Crashed at:** {CrashPoint:0.00}x\n**Result:** You lost **{Bet:N0)}** credits"
                                )
                                .WithColor(Color.Red)
                                .Build();
                            x.Components = new ComponentBuilder().Build();
                        }
                    )
                    .ConfigureAwait(false);
                await OnGameEndedAsync(new GameEndEventArgs(UserId, Bet, 0, GameResult.Lose)).ConfigureAwait(false);
                break;
            }

            await Task.Delay(2000).ConfigureAwait(false);
        }
    }

    public async Task StopAsync()
    {
        TokenSource.Cancel();
        await Message
            .ModifyAsync(
                x =>
                {
                    x.Embed = new CrashEmbedBuilder(
                            this,
                            $"**Stopped at:** {Multiplier:0.00}x\n"
                            + $"**Result:** You win **{Profit.ToString("N0", CultureInfo.InvariantCulture)}** credits"
                        )
                        .WithColor(Color.Green)
                        .Build();
                    x.Components = new ComponentBuilder().Build();
                }
            )
            .ConfigureAwait(false);
        await OnGameEndedAsync(new GameEndEventArgs(UserId, Bet, Profit, GameResult.Win)).ConfigureAwait(false);
    }

    private Task OnGameEndedAsync(GameEndEventArgs e)
    {
        return GameEnded.Invoke(this, e);
    }
}