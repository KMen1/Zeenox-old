﻿using System.Threading.Tasks;
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
                .WithButton("Filterek ki", "clearfilters", emote: new Emoji("🗑️"), row: 1)
                .WithButton("Hangerő fel", "volumeup", emote: new Emoji("🔊"), row: 1)
                .WithSelectMenu(
                    new SelectMenuBuilder()
                        .WithPlaceholder("Filterek kiválasztása")
                        .WithCustomId("filterselectmenu")
                        .WithMinValues(1)
                        .WithMaxValues(4)
                        .AddOption("Basszus Erősítés", "1", "Basszus Erősítése")
                        .AddOption("Nightcore", "2", "Nightcore hanghatás")
                        .AddOption("8D", "8", "8D hanghatás")
                        .AddOption("Vaporwave", "4", "Vaporwave hanghatás")
                    , 2);
            return component.Build(); 
        }));
    }
}