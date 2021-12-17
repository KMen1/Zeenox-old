using System.Threading.Tasks;
using Discord;

namespace KBot.Commands.Voice;

public static class MakeVoice
{
    public static Task<SlashCommandBuilder[]> MakeVoiceCommands()
    {
        return Task.Run(() =>
        {
            var newCommands = new[]
            {
                new SlashCommandBuilder()
                    .WithName("join")
                    .WithDescription("Csatlakozik ahhoz a hangcsatornához, amelyben tartózkodsz"),
                new SlashCommandBuilder()
                    .WithName("leave")
                    .WithDescription("Elhagyja azt a hangcsatornát, amelyben a bot jelenleg tartózkodik"),
                new SlashCommandBuilder()
                    .WithName("play")
                    .WithDescription("Lejátssza a kívánt zenét")
                    .AddOption("query", ApplicationCommandOptionType.String, "Zene linkje vagy címe", true),
                new SlashCommandBuilder()
                    .WithName("stop")
                    .WithDescription("Zenelejátszás megállítása"),
                new SlashCommandBuilder()
                    .WithName("move")
                    .WithDescription("Átlép abba a hangcsatornába, amelyben tartózkodsz"),
                new SlashCommandBuilder()
                    .WithName("skip")
                    .WithDescription("Lejátsza a következő zenét a sorban"),
                new SlashCommandBuilder()
                    .WithName("pause")
                    .WithDescription("Zenelejátszás szüneteltetése"),
                new SlashCommandBuilder()
                    .WithName("resume")
                    .WithDescription("Zenelejátszás folytatása"),
                new SlashCommandBuilder()
                    .WithName("volume")
                    .WithDescription("Hangerő beállítása")
                    .AddOption("volume", ApplicationCommandOptionType.Integer, "Hangerő számban megadva (1-100)", true,
                        minValue: 1, maxValue: 100),
                new SlashCommandBuilder()
                    .WithName("queue")
                    .WithDescription("A sorban lévő zenék listája"),
                new SlashCommandBuilder()
                    .WithName("clearqueue")
                    .WithDescription("A sorban lévő zenék törlése"),
                new SlashCommandBuilder()
                    .WithName("bassboost")
                    .WithDescription("Basszus erősítés bekapcsolása"),
                new SlashCommandBuilder()
                    .WithName("nightcore")
                    .WithDescription("Nightcore mód bekapcsolása"),
                new SlashCommandBuilder()
                    .WithName("8d")
                    .WithDescription("8D mód bekapcsolása"),
                new SlashCommandBuilder()
                    .WithName("vaporwave")
                    .WithDescription("Vaporwave mód bekapcsolása"),
                new SlashCommandBuilder()
                    .WithName("speed")
                    .WithDescription("Zene sebességének növelése")
                    .AddOption("speed", ApplicationCommandOptionType.Integer, "Sebesség számban megadva (1-10)", true,
                        minValue: 1, maxValue: 10),
                new SlashCommandBuilder()
                    .WithName("pitch")
                    .WithDescription("Zene hangmagasságának növelése")
                    .AddOption("pitch", ApplicationCommandOptionType.Integer, "Hangmagasság számban megadva (1-10)",
                        true, minValue: 1, maxValue: 10),
                new SlashCommandBuilder()
                    .WithName("loop")
                    .WithDescription("Zene ismétlésének bekapcsolása / kikapcsolása"),
                new SlashCommandBuilder()
                    .WithName("clearfilter")
                    .WithDescription("Minden aktív szűrőt deaktivál")
            };
            return newCommands;
        });
    }
}