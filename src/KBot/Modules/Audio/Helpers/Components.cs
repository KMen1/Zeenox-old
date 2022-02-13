using Discord;
using Victoria;
using Victoria.Enums;

namespace KBot.Modules.Audio.Helpers;

public static class Components
{
    public static MessageComponent NowPlayingComponents(bool canGoBack, bool canGoForward, LavaPlayer player)
    {
        var component = new ComponentBuilder()
            .WithButton("Előző", "previous", emote: new Emoji("⏮"), disabled: !canGoBack, row: 0)
            .WithButton(player.PlayerState == PlayerState.Paused ? "Folytatás" : "Szüneteltetés", "pause", emote: new Emoji("⏸"), row: 0)
            .WithButton("Leállítás", "stop", emote: new Emoji("⏹"), row: 0, style: ButtonStyle.Danger)
            .WithButton("Következő", "next", emote: new Emoji("⏭"), disabled: !canGoForward, row: 0)
            .WithButton("Hangerő Le", "volumedown", emote: new Emoji("🔉"), row: 1, disabled: player.Volume == 0)
            .WithButton("Ismétlés", "repeat", emote: new Emoji("🔁"), row: 1)
            .WithButton("Szűrő ki", "clearfilters", emote: new Emoji("🗑️"), row: 1)
            .WithButton("Hangerő fel", "volumeup", emote: new Emoji("🔊"), row: 1, disabled: player.Volume == 100)
            .WithSelectMenu(
                new SelectMenuBuilder()
                    .WithPlaceholder("Szűrő kiválasztása")
                    .WithCustomId("filterselectmenu")
                    .WithMinValues(1)
                    .WithMaxValues(1)
                    .AddOption("Basszus Erősítés", "bassboost")
                    .AddOption("Pop hanghatás", "pop")
                    .AddOption("Soft hanghatás", "soft")
                    .AddOption("Treblebass hanghatás", "treblebass")
                    .AddOption("Nightcore hanghatás", "nightcore")
                    .AddOption("8D hanghatás", "eightd")
                    .AddOption("China hanghatás", "china")
                    .AddOption("Vaporwave hanghatás", "vaporwave")
                    .AddOption("Doubletime hanghatás", "doubletime")
                    .AddOption("Slowmotion hanghatás", "slowmotion")
                    .AddOption("Chipmunk hanghatás", "chipmunk")
                    .AddOption("Darthvader hanghatás", "darthvader")
                    .AddOption("Dance hanghatás", "dance")
                    .AddOption("Vibrate hanghatás", "vibrate")
                    .AddOption("Vibrato hanghatás", "vibrato")
                    .AddOption("Tremolo hanghatás", "tremolo")
                , 2).Build();
        return component;
    }
}