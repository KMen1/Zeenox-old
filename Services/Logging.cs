using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

namespace KBot.Services;

public class Logging
{
    private readonly DiscordSocketClient _client;
    private readonly LavaNode _lavaNode;
    private readonly SemaphoreSlim _semaphoreSlim;

    public Logging(IServiceProvider services)
    {
        _semaphoreSlim = new SemaphoreSlim(1);
        _client = services.GetRequiredService<DiscordSocketClient>();
        _lavaNode = services.GetRequiredService<LavaNode>();
    }

    public Task InitializeAsync()
    {
        _client.Log += _client_Log;
        _lavaNode.OnLog += _lavaNode_OnLog;
        return Task.CompletedTask;
    }

    private Task _lavaNode_OnLog(LogMessage arg)
    {
        return Log(arg);
    }

    private Task _client_Log(LogMessage arg)
    {
        return Log(arg);
    }

    private async Task Log(LogMessage logMessage)
    {
        await _semaphoreSlim.WaitAsync();

        Console.ForegroundColor = logMessage.Severity switch
        {
            LogSeverity.Critical => ConsoleColor.Red,
            LogSeverity.Error => ConsoleColor.Red,
            LogSeverity.Warning => ConsoleColor.Yellow,
            LogSeverity.Info => ConsoleColor.Green,
            LogSeverity.Verbose => ConsoleColor.Blue,
            LogSeverity.Debug => ConsoleColor.Gray,
            _ => Console.ForegroundColor
        };

        var time = DateTime.Now.ToString();
        if (logMessage.Exception == null)
        {
            Console.WriteLine($"[{time}] [{logMessage.Severity,7}] : ({logMessage.Source,7}) : {logMessage.Message}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(logMessage.Exception.ToString());
        }

        _semaphoreSlim.Release();
    }
}