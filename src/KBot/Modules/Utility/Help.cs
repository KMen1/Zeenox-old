using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace KBot.Modules.Utility;

public class HelpCommand : InteractionModuleBase<SocketInteractionContext>
{
    public DiscordSocketClient Client { get; set; }

    [SlashCommand("help", "Kilistázza az összes parancsot")]
    public async Task Help(
        [Summary("command", "Egy adott parancsra vonatkozó információt ad vissza")] string optCommand = "")
    {
        var guildCommands = await Context.Guild.GetApplicationCommandsAsync().ConfigureAwait(false);

        var combinedString = string.Join(", ", guildCommands.Select(command => command.Name).ToArray());

        var eb = new EmbedBuilder
        {
            Title = "**Elérhető parancsok**",
            Description =
                "Összes parancs listázása ( help <parancs> )-al több információt tudhatsz meg az adott parancsról!",
            Color = Color.Orange,
            Timestamp = DateTime.Today,
            ThumbnailUrl = Client.CurrentUser.GetAvatarUrl()
        };

        if (optCommand != "")
        {
            var reqCommand = guildCommands.FirstOrDefault(x => string.Equals(x.Name,
                optCommand, StringComparison.CurrentCultureIgnoreCase));

            if (reqCommand?.Name == optCommand)
            {
                eb.WithTitle($"**{reqCommand?.Name[0].ToString().ToUpper() + reqCommand?.Name[1..]}**");
                eb.WithDescription($"`{reqCommand?.Description}`");
            }
            else
            {
                await RespondAsync($"Nincs ilyen parancs - `{optCommand}`").ConfigureAwait(false);
            }
        }
        else
        {
            eb.AddField("Parancsok", $"`{combinedString}`");
        }

        if (Context.Channel is SocketDMChannel)
        {
            await RespondAsync(embed: eb.Build()).ConfigureAwait(false);
        }
        else
        {
            await Context.User.SendMessageAsync(embed: eb.Build()).ConfigureAwait(false);
            await RespondAsync(
                $":exclamation: **{Context.User.Mention}** Nézd meg a privát üzeneteidet :exclamation: ")
                .ConfigureAwait(false);
        }
    }
}