using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Victoria;

namespace KBot.Modules.Audio.Helpers;

public static class Embeds
{
    private const string SuccessIcon = "https://i.ibb.co/HdqsDXh/tick.png";
    private const string ErrorIcon = "https://i.ibb.co/SrZZggy/x.png";
    private const string PlayingGif = "https://bestanimations.com/media/discs/895872755cd-animated-gif-9.gif";

    public static Embed LeaveEmbed(IVoiceChannel vChannel)
    {
        return new EmbedBuilder()
            .WithAuthor("SIKERES ELHAGYÁS", SuccessIcon)
            .WithDescription($"A következő csatornából: {vChannel.Mention}")
            .WithColor(Color.Green)
            .Build();
    }

    public static Embed MoveEmbed(IVoiceChannel vChannel)
    {
        return new EmbedBuilder()
            .WithAuthor("SIKERES MOZGATÁS", SuccessIcon)
            .WithDescription($"A következő csatornába: {vChannel.Mention}")
            .WithColor(Color.Green)
            .Build();
    }

    public static async ValueTask<Embed> NowPlayingEmbed(IUser user, LavaPlayer player, bool isloopEnabled, string filter, int queueLength)
    {
        var eb = new EmbedBuilder()
            .WithAuthor("MOST JÁTSZOTT", PlayingGif)
            .WithTitle(player.Track.Title)
            .WithUrl(player.Track.Url)
            .WithImageUrl(await player.Track.FetchArtworkAsync().ConfigureAwait(false))
            .WithColor(Color.Green)
            .AddField("👨 Hozzáadta", user.Mention, true)
            .AddField("🔼 Feltöltötte", $"`{player.Track.Author}`", true)
            .AddField("🎙️ Csatorna", player.VoiceChannel.Mention, true)
            .AddField("🕐 Hosszúság", $"`{player.Track.Duration:hh\\:mm\\:ss}`", true)
            .AddField("🔁 Ismétlés", isloopEnabled ? "`Igen`" : "`Nem`", true)
            .AddField("🔊 Hangerő", $"`{player.Volume.ToString()}%`", true)
            .AddField("📝 Szűrő", filter is not null ? $"`{filter}`" : "`Nincs`", true)
            .AddField("🎶 Várólistán", $"`{queueLength.ToString()}`", true)
            .Build();
        return await new ValueTask<Embed>(eb).ConfigureAwait(false);
    }

    public static Embed VolumeEmbed(LavaPlayer player)
    {
        return new EmbedBuilder()
            .WithAuthor($"HANGERŐ {player.Volume.ToString()}%-RA ÁLLÍTVA", SuccessIcon)
            .WithDescription($"Ebben a csatornában: {player.VoiceChannel.Mention}")
            .WithColor(Color.Green)
            .Build();
    }

    public static Embed QueueEmbed(LavaPlayer player, LinkedList<(LavaTrack track, SocketUser user)> queue, bool cleared = false)
    {
        var eb = new EmbedBuilder()
            .WithAuthor(cleared ? "LEJÁTSZÁSI LISTA TÖRÖLVE" : "LEJÁTSZÁSI LISTA LEKÉRVE", SuccessIcon)
            .WithDescription($"Ebben a csatornában: {player.VoiceChannel.Mention}")
            .WithColor(Color.Green);
        if (cleared)
        {
            return eb.Build();
        }
        if (queue.Count == 0)
        {
            eb.WithDescription("`Nincs zene a lejátszási listában`");
        }
        else
        {
            var desc = new StringBuilder();
            foreach (var track in queue)
            {
                desc.AppendLine(
                    $":{(queue.TakeWhile(n => n != track).Count() + 1).ToWords()}: [`{track.track.Title}`]({track.track.Url}) | Hozzáadta: {track.user.Mention}");
            }

            eb.WithDescription(desc.ToString());
        }
        return eb.Build();
    }

    public static Embed AddedToQueueEmbed(List<LavaTrack> tracks)
    {
        var desc = new StringBuilder();
        foreach (var track in tracks.Take(10))
        {
            desc.AppendLine(
                $"{tracks.TakeWhile(n => n != track).Count() + 1}. [`{track.Title}`]({track.Url})");
        }
        if (tracks.Count > 10)
        {
            desc.Append("és még ").Append(tracks.Count - 10).AppendLine(" zene");
        }

        return new EmbedBuilder()
            .WithAuthor($"{tracks.Count} SZÁM HOZZÁADVA A VÁRÓLISTÁHOZ", SuccessIcon)
            .WithColor(Color.Orange)
            .WithDescription(desc.ToString())
            .Build();
    }

    public static Embed ErrorEmbed(string exception)
    {
        return new EmbedBuilder()
            .WithAuthor("HIBA", ErrorIcon)
            .WithTitle("Hiba történt a parancs végrehajtása során")
            .WithDescription("Kérlek próbáld meg újra! \n" +
                             "Ha a hiba továbbra is fennáll, kérlek jelezd a <@132797923049209856>-nek!")
            .WithColor(Color.Red)
            .AddField("Hibaüzenet", $"```{exception}```")
            .Build();
    }
}