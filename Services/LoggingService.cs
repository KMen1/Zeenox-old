using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Serilog;
using Victoria;

namespace KBot.Services;

public class LoggingService : DiscordClientService
{
    
    private readonly LavaNode _lavaNode;
    public LoggingService(DiscordSocketClient client, ILogger<LoggingService> logger, LavaNode lavaNode) : base(client, logger)
    {
        _lavaNode = lavaNode;
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _lavaNode.OnLog += LogAsync;
        return Task.CompletedTask;
    }

    private Task LogAsync(LogMessage arg)
    {
        switch (arg.Severity)
        {
            case LogSeverity.Critical:
                Log.Logger.Fatal(arg.Exception, arg.Message);
                break;
            case LogSeverity.Error:
                Log.Logger.Error(arg.Exception, arg.Message);
                break;
            case LogSeverity.Warning:
                Log.Logger.Error(arg.Exception, arg.Message);
                break;
            case LogSeverity.Info:
                Log.Logger.Information(arg.Exception, arg.Message);
                break;
            case LogSeverity.Verbose:
                Log.Logger.Verbose(arg.Exception, arg.Message);
                break;
            case LogSeverity.Debug:
                Log.Logger.Debug(arg.Exception, arg.Message);
                break;
        }
        return Task.CompletedTask;
    }
}