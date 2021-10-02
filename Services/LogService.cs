using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Victoria;

namespace KBot.Services
{
    public class LogService
    {
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly LavaNode _lavaNode;

        public LogService(IServiceProvider services)
        {
            _semaphoreSlim = new SemaphoreSlim(1);
            _client = services.GetRequiredService<DiscordSocketClient>();
            _commandService = services.GetRequiredService<CommandService>();
            _lavaNode = services.GetRequiredService<LavaNode>();
        }

        public Task InitializeAsync()
        {
            _client.Log += _client_Log;
            _commandService.Log += _command_Log;
            _lavaNode.OnLog += _lavaNode_OnLog;
            return Task.CompletedTask;
        }

        private Task _lavaNode_OnLog(LogMessage arg) => Log(arg);
        private Task _command_Log(LogMessage arg) => Log(arg);
        private Task _client_Log(LogMessage arg) => Log(arg);

        public async Task Log(LogMessage logMessage)
        {
            await _semaphoreSlim.WaitAsync();

            switch (logMessage.Severity)
            {
                case LogSeverity.Critical:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case LogSeverity.Verbose:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }

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
}
