using Discord;
using Lavalink4NET.Player;

namespace KBot.Modules.Audio.Helpers;

public static class Components
{
    public static MessageComponent NowPlayingComponents(MusicPlayer player)
    {
        return new ComponentBuilder()
            .WithButton(" ", "previous", emote: new Emoji("⏮"), disabled: !player.CanGoBack, row: 0)
            .WithButton(" ", "pause", emote: player.State == PlayerState.Playing ? new Emoji("⏸") : new Emoji("▶"), row: 0)
            .WithButton(" ", "stop", emote: new Emoji("⏹"), row: 0, style: ButtonStyle.Danger)
            .WithButton(" ", "next", emote: new Emoji("⏭"), disabled: !player.CanGoForward, row: 0)
            .WithButton(" ", "volumedown", emote: new Emoji("🔉"), row: 1, disabled: player.Volume == 0)
            .WithButton(" ", "repeat", emote: new Emoji("🔁"), row: 1)
            .WithButton(" ", "clearfilters", emote: new Emoji("🗑️"), row: 1)
            .WithButton(" ", "volumeup", emote: new Emoji("🔊"), row: 1, disabled: player.Volume == 1.0f)
            .WithSelectMenu(new SelectMenuBuilder()
                    .WithPlaceholder("Szűrő kiválasztása")
                    .WithCustomId("filterselectmenu")
                    .WithMinValues(1)
                    .WithMaxValues(1)
                    .AddOption("Basszus Erősítés", "bassboost")
                    .AddOption("Pop", "pop")
                    .AddOption("Lágy", "soft")
                    .AddOption("Hangos", "treblebass")
                    .AddOption("Nightcore", "nightcore")
                    .AddOption("8D", "eightd")
                    .AddOption("Kínai", "china")
                    .AddOption("Vaporwave", "vaporwave")
                    .AddOption("Gyorsítás", "doubletime")
                    .AddOption("Lassítás", "slowmotion")
                    .AddOption("Alvin és a mókusok", "chipmunk")
                    .AddOption("Darthvader", "darthvader")
                    .AddOption("Tánc", "dance")
                    .AddOption("Vibrato hanghatás", "vibrato")
                    .AddOption("Tremolo hanghatás", "tremolo"), 2)
            .Build();
    }
}