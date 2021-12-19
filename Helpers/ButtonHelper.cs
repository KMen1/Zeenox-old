using System.Threading.Tasks;
using Discord;

namespace KBot.Helpers;

public static class ButtonHelper
{
    public static Task<MessageComponent> MakeNowPlayingButtons(bool enableback, bool enableforward, bool resumed)
    {
        return Task.Run(() =>
        {
            var buttons = new ComponentBuilder()
                .WithButton("Előző", "back", emote:new Emoji("⏮"), disabled:!enableback, row:0)
                .WithButton(resumed ? "Szüneteltetés" : "Folytatás", "pause", emote:new Emoji("⏸"), row:0)
                .WithButton("Következő", "next", emote:new Emoji("⏭"), disabled:!enableforward, row:0)
                .WithButton("Hangerő Le", "volumedown", emote:new Emoji("🔉"), row: 1)
                .WithButton("Hangerő fel", "volumeup", emote:new Emoji("🔊"), row: 1).Build();
            return buttons;
        });
    }
}