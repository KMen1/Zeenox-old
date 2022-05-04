using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Discord;
using Lavalink4NET.Player;

namespace KBot.Modules.Music.Embeds;

public class AddedToQueueEmbedBuilder : EmbedBuilder
{
    public AddedToQueueEmbedBuilder(IEnumerable<LavalinkTrack> tracks)
    {
        var enumerable = tracks.ToList();
        var desc = enumerable
            .Take(10)
            .Aggregate(
                "",
                (current, track) =>
                    current
                    + $"{enumerable.TakeWhile(n => n != track).Count() + 1}. [`{track.Title}`]({track.Source})\n"
            );
        if (enumerable.Count > 10)
            desc += $"and {(enumerable.Count - 10).ToString(CultureInfo.InvariantCulture)} more\n";
        Title = $"Queued {enumerable.Count} tracks";
        Color = Discord.Color.Green;
        Description = desc;
    }
}
