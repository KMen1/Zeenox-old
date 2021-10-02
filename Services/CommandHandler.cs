using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace KBot.Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _config;

        public CommandHandler(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _commandService = services.GetRequiredService<CommandService>();
            _config = services.GetRequiredService<IConfiguration>();
            _services = services;
        }

        public async Task InitializeAsync()
        {
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _client.MessageReceived += HandleCommands;
        }

        private async Task HandleCommands(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            var context = new SocketCommandContext(_client, message);
            if (message.Channel.GetType() == typeof(SocketDMChannel) && !message.Content.StartsWith(_config["Prefix"] + "help")) return;
            if (message.Author.IsBot == true) return;
            int ArgPos = 0;

            if (message.HasStringPrefix(_config["Prefix"], ref ArgPos))
            {
                var result = await _commandService.ExecuteAsync(context, ArgPos, _services);
                if (!result.IsSuccess)
                {
                    EmbedBuilder eb = new EmbedBuilder
                    {
                        Title = $"Hiba történt a következő parancs lefuttatásakor: `{context.Message.Content}`",
                        Description = $"`{result.Error + ": " + result.ErrorReason}`",
                        Color = Color.Red
                    };
                    await context.Channel.SendMessageAsync(string.Empty, false, eb.Build());
                } 
            }
        }
    }
}
