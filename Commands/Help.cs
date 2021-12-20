using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace KBot.Commands;

public class HelpCommand : InteractionModuleBase<InteractionContext>
{
    public DiscordSocketClient Client { get; set; }

    [SlashCommand("help", "Kilistázza az összes parancsot")]
    public async Task Help(
        [Summary("command", "Egy adott parancsra vonatkozó információt ad vissza")] string optCommand = "")
    {
        var guild = Context.Guild;
        var guildCommands = await guild.GetApplicationCommandsAsync();

        var combinedString = string.Join(", ", guildCommands.Select(command => command.Name).ToArray());

        var eb = new EmbedBuilder
        {
            Title = "**Elérhető parancsok**",
            Description =
                "Összes parancs listázása ( help <parancs> )-al több információt tudhatsz meg az adott parancsról!",
            Color = Color.Orange,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(1575900037337),
            ThumbnailUrl = Client.CurrentUser.GetAvatarUrl()
        };

        if (optCommand != "")
        {
            var reqCommand = guildCommands.FirstOrDefault(x => string.Equals(x.Name,
                optCommand, StringComparison.CurrentCultureIgnoreCase));

            if (reqCommand?.Name == optCommand)
            {
                eb.WithTitle($"**{reqCommand?.Name.First().ToString().ToUpper() + reqCommand?.Name[1..]}**");
                eb.WithDescription($"`{reqCommand?.Description}`");
            }
            else
            {
                await RespondAsync($"Nincs ilyen parancs - `{optCommand}`");
            }
        }
        else
        {
            eb.WithFooter(footer =>
                footer.WithText($"Requested by: {Context.User.Username}")
                    .WithIconUrl(Context.User.GetAvatarUrl()));
            eb.AddField("Parancsok", $"`{combinedString}`");
        }

        if (Context.Channel is SocketDMChannel)
        {
            await RespondAsync(embed: eb.Build());
        }
        else
        {
            await Context.User.SendMessageAsync(embed: eb.Build());
            await RespondAsync(
                $":exclamation: **{Context.User.Mention}** Nézd meg a privát üzeneteidet :exclamation: ");
        }
    }
}