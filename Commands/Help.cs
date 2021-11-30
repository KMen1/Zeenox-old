using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KBot.Commands
{
    public class Help
    {
        private readonly DiscordSocketClient _client;
        
        public Help(DiscordSocketClient client)
        {
            _client = client;
        }

        public async Task  MakeHelpCommand()
        {
            var options = new SlashCommandOptionBuilder()
                .WithName("command")
                .WithType(ApplicationCommandOptionType.String)
                .WithDescription("");
            var helpCommand = new SlashCommandBuilder()
                .WithName("help")
                .WithDescription("Kilistázza az összes parancsot")
                .AddOption(options);


            var globalCommands = await _client.GetGlobalApplicationCommandsAsync();

            List<string> CommandsName = new List<string>();
            foreach (var command in globalCommands)
            {
                CommandsName.Add(command.Name);
                options.AddChoice(command.Name, command.Name);
            }

            if (!CommandsName.Contains(helpCommand.Name))
                await _client.CreateGlobalApplicationCommandAsync(helpCommand.Build());
        }

        public async Task HandleHelpCommand(SocketSlashCommand slashCommand)
        {
            var globalCommands = await _client.GetGlobalApplicationCommandsAsync();
            List<SocketApplicationCommand> commands = new List<SocketApplicationCommand>();

            List<string> CommandsName = new List<string>();
            foreach (var command in globalCommands)
            {
                CommandsName.Add(command.Name);
            }
            string combindedString = string.Join(", ", CommandsName.ToArray());

            EmbedBuilder eb = new EmbedBuilder
            {
                Title = "**Elérhető parancsok**",
                Description = "Összes parancs listázása ( help <parancs> )-al több információt tudhatsz meg az adott parancsról!",
                Color = Color.Orange,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(1575900037337),
                ThumbnailUrl = _client.CurrentUser.GetAvatarUrl(ImageFormat.Auto)
            };

            if (!slashCommand.Data.Options.First().Value.Equals(""))
            {
                var co = globalCommands.FirstOrDefault(x => x.Name.ToLower() == slashCommand.Data.Options.First().Value.ToString().ToLower());

                if (co.Name == slashCommand.Data.Options.First().Value.ToString().ToLower())
                {
                    eb.WithTitle($"**{co.Name.First().ToString().ToUpper() + co.Name[1..]}**");
                    eb.WithDescription($"`{co.Description}`");
                }
                else
                {
                    await slashCommand.RespondAsync($"Nincs ilyen parancs - `{slashCommand.Data.Options.First().Value.ToString().ToLower()}`");
                }
            
            }
            else
            {
                eb.WithFooter(footer => footer.WithText($"Requested by: {slashCommand.User.Username}").WithIconUrl(slashCommand.User.GetAvatarUrl(ImageFormat.Auto)));
                eb.AddField("Parancsok", $"`{combindedString}`");
            }

            if (slashCommand.Channel is SocketDMChannel)
            {
                await slashCommand.RespondAsync(embed: eb.Build());
            }
            else
            {
                await slashCommand.User.SendMessageAsync(null, false, eb.Build());
                await slashCommand.RespondAsync($":exclamation: **{slashCommand.User.Mention}** Nézd meg a privát üzeneteidet :exclamation: ");
            }
        }
    }
}
