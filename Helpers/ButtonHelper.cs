using System.Threading.Tasks;
using Discord;

namespace KBot.Helpers;

public static class ButtonHelper
{
    public static Task<MessageComponent> MakeNowPlayingButtons(bool canGoBack, bool canGoForward, bool isPlaying, bool isLoopEnabled)
    {
        return Task.Run(() =>
        {
            var buttons = new ComponentBuilder()
                .WithButton("Előző", "previous", emote: new Emoji("⏮"), disabled: !canGoBack, row: 0)
                .WithButton(isPlaying ? "Szüneteltetés" : "Folytatás", "pause", emote: new Emoji("⏸"), row: 0)
                .WithButton("Következő", "next", emote: new Emoji("⏭"), disabled: !canGoForward, row: 0)
                .WithButton("Hangerő Le", "volumedown", emote: new Emoji("🔉"), row: 1)
                .WithButton("Ismétlés", "repeat", emote: new Emoji("🔁"), row: 1, disabled: isLoopEnabled)
                .WithButton("Hangerő fel", "volumeup", emote: new Emoji("🔊"), row: 1)
                .Build();
            return buttons;
        });
    }
}