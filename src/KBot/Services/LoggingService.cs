using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Lavalink4NET.Logging;

namespace KBot.Services;

public class LoggingService : IInjectable
{
    public LoggingService(InteractionService interactionService, ILogger lavaLogger)
    {
        interactionService.Log += LogAsync;
        ((EventLogger) lavaLogger).LogMessage += Log;
    }

    private static void Log(object? sender, LogMessageEventArgs arg)
    {
        switch (arg.Level)
        {
            case LogLevel.Error:
                Serilog.Log.Logger.Error(arg.Exception, arg.Message);
                break;
            case LogLevel.Warning:
                Serilog.Log.Logger.Error(arg.Exception, arg.Message);
                break;
            case LogLevel.Information:
                Serilog.Log.Logger.Information(arg.Exception, arg.Message);
                break;
            case LogLevel.Trace:
                Serilog.Log.Logger.Verbose(arg.Exception, arg.Message);
                break;
            case LogLevel.Debug:
                Serilog.Log.Logger.Debug(arg.Exception, arg.Message);
                break;
        }
    }

    private static Task LogAsync(LogMessage arg)
    {
        switch (arg.Severity)
        {
            case LogSeverity.Critical:
                Serilog.Log.Logger.Fatal(arg.Exception, arg.Message);
                break;
            case LogSeverity.Error:
                Serilog.Log.Logger.Error(arg.Exception, arg.Message);
                break;
            case LogSeverity.Warning:
                Serilog.Log.Logger.Error(arg.Exception, arg.Message);
                break;
            case LogSeverity.Info:
                Serilog.Log.Logger.Information(arg.Exception, arg.Message);
                break;
            case LogSeverity.Verbose:
                Serilog.Log.Logger.Verbose(arg.Exception, arg.Message);
                break;
            case LogSeverity.Debug:
                Serilog.Log.Logger.Debug(arg.Exception, arg.Message);
                break;
        }

        return Task.CompletedTask;
    }
}