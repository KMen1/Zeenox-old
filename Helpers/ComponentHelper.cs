using System.Threading.Tasks;
using Discord;

namespace KBot.Helpers;

public static class ComponentHelper
{
    public static Task<MessageComponent> MakeNowPlayingComponents(bool canGoBack, bool canGoForward, bool isPlaying)
    {
        return Task.Run((() =>
        {
            var component = new ComponentBuilder()
                .WithButton("Előző", "previous", emote: new Emoji("⏮"), disabled: !canGoBack, row: 0)
                .WithButton(isPlaying ? "Szüneteltetés" : "Folytatás", "pause", emote: new Emoji("⏸"), row: 0)
                .WithButton("Leállítás", "stop", emote: new Emoji("⏹"), row: 0, style: ButtonStyle.Danger)
                .WithButton("Következő", "next", emote: new Emoji("⏭"), disabled: !canGoForward, row: 0)
                .WithButton("Hangerő Le", "volumedown", emote: new Emoji("🔉"), row: 1)
                .WithButton("Ismétlés", "repeat", emote: new Emoji("🔁"), row: 1)
                .WithButton("Szűrők ki", "clearfilters", emote: new Emoji("🗑️"), row: 1)
                .WithButton("Hangerő fel", "volumeup", emote: new Emoji("🔊"), row: 1)
                .WithSelectMenu(
                    new SelectMenuBuilder()
                        .WithPlaceholder("Szűrők kiválasztása")
                        .WithCustomId("filterselectmenu")
                        .WithMinValues(1)
                        .WithMaxValues(4)
                        .AddOption("Basszus Erősítés", "bassboost")
                        .AddOption("Nightcore hanghatás", "nightcore")
                        .AddOption("8D hanghatás", "eightd")
                        .AddOption("Vaporwave hanghatás", "vaporwave")
                    , 2);
            return component.Build(); 
        }));
    }
}