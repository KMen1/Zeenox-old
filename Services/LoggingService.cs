using System;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

namespace KBot.Services;

public class LogService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly LavaNode _lavaNode;
    private readonly SemaphoreSlim _semaphoreSlim;

    public LogService(IServiceProvider services)
    {
        _semaphoreSlim = new SemaphoreSlim(1);
        _client = services.GetRequiredService<DiscordSocketClient>();
        _lavaNode = services.GetRequiredService<LavaNode>();
        _interactionService = services.GetRequiredService<InteractionService>();
    }

    public void Initialize()
    {
        _client.Log += LogEventAsync;
        _lavaNode.OnLog += LogEventAsync;
        _interactionService.Log += LogEventAsync;
    }

    private Task LogEventAsync(LogMessage arg)
    {
        return LogAsync(arg);
    }

    private async Task LogAsync(LogMessage logMessage)
    {
        await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

        Console.ForegroundColor = logMessage.Severity switch
        {
            LogSeverity.Critical => ConsoleColor.Red,
            LogSeverity.Error => ConsoleColor.DarkRed,
            LogSeverity.Warning => ConsoleColor.Yellow,
            LogSeverity.Info => ConsoleColor.Green,
            LogSeverity.Verbose => ConsoleColor.DarkGray,
            LogSeverity.Debug => ConsoleColor.Gray,
            _ => ConsoleColor.White
        };

        var time = DateTime.Now.ToString(CultureInfo.CurrentCulture);
        Console.WriteLine(logMessage.Exception is null
            ? $"[{time}] [{logMessage.Severity,7}] : ({logMessage.Source,7}) : {logMessage.Message}"
            : logMessage.Exception.ToString());

        _semaphoreSlim.Release();
    }
}