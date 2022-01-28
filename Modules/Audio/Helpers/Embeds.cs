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
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = "SIKERES ELHAGYÁS",
                IconUrl = SuccessIcon
            },
            Description = $"A következő csatornából: `{vChannel.Name}`",
            Color = Color.Green
        }.Build();
        return eb;
    }

    public static Embed MoveEmbed(IVoiceChannel vChannel)
    {
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = "SIKERES ÁTHELYEZÉS",
                IconUrl = SuccessIcon
            },
            Description = $"A következő csatornába: `{vChannel.Name}`",
            Color = Color.Green
        }.Build();
        return eb;
    }

    public static async ValueTask<Embed> NowPlayingEmbed(IUser user, LavaPlayer player, bool isloopEnabled, string filter, int queueLength)
    {
        var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = "MOST JÁTSZOTT",
                    IconUrl = PlayingGif
                },
                Title = player.Track.Title,
                Url = player.Track.Url,
                ImageUrl = await player.Track.FetchArtworkAsync().ConfigureAwait(false),
                Color = Color.Green,
                Fields = new List<EmbedFieldBuilder>
                {
                    new()
                    {
                        Name = "👨 Hozzáadta",
                        Value = user.Mention,
                        IsInline = true
                    },
                    new()
                    {
                        Name = "🔼 Feltöltötte",
                        Value = $"`{player.Track.Author}`",
                        IsInline = true
                    },
                    new()
                    {
                        Name = "🎙️ Csatorna",
                        Value = $"`{player.VoiceChannel.Name}`",
                        IsInline = true
                    },
                    new()
                    {
                        Name = "🕐 Hosszúság",
                        Value = $"`{player.Track.Duration:hh\\:mm\\:ss}`",
                        IsInline = true
                    },
                    new()
                    {
                        Name = "🔁 Ismétlés",
                        Value = isloopEnabled ? "`Bekapcsolva`" : "`Kikapcsolva`",
                        IsInline = true
                    },
                    new()
                    {
                        Name = "🔊 Hangerő",
                        Value = $"`{player.Volume.ToString()}%`",
                        IsInline = true
                    },
                    new()
                    {
                        Name = "📝 Szűrő",
                        Value = filter is not null ? $"`{filter}`": "`Nincs`",
                        IsInline = true
                    },
                    new()
                    {
                        Name = "🎶 Várólistán",
                        Value = $"`{queueLength.ToString()}`",
                        IsInline = true
                    },
                }
            }.Build();
        return await new ValueTask<Embed>(eb).ConfigureAwait(false);
    }

    public static Embed VolumeEmbed(LavaPlayer player)
    {
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = $"HANGERŐ {player.Volume.ToString()}%-RA ÁLLÍTVA",
                IconUrl = SuccessIcon
            },
            Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
            Color = Color.Green,
        }.Build();
        return eb;
    }

    public static Embed QueueEmbed(LavaPlayer player, LinkedList<(LavaTrack track, SocketUser user)> queue, bool cleared = false)
    {
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = cleared ? "LEJÁTSZÁSI LISTA TÖRÖLVE" : "LEJÁTSZÁSI LISTA LEKÉRVE",
                IconUrl = SuccessIcon
            },
            Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
            Color = Color.Green
        };
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
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = $"{tracks.Count} SZÁM HOZZÁADVA A VÁRÓLISTÁHOZ",
                IconUrl = SuccessIcon
            },
            Color = Color.Orange
        };
        var desc = new StringBuilder();
        foreach (var track in tracks.Take(10))
        {
            desc.AppendLine(
                $"{tracks.TakeWhile(n => n != track).Count() + 1} [`{track.Title}`]({track.Url})`");
        }
        eb.WithDescription(desc.ToString());
        return eb.Build();
    }

    public static Embed ErrorEmbed(string exception)
    {
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = "HIBA",
                IconUrl = ErrorIcon
            },
            Title = "Hiba történt a parancs végrehajtása során",
            Description = "Kérlek próbáld meg újra! \n" +
                          "Ha a hiba továbbra is fennáll, kérlek jelezd a <@132797923049209856>-nek!",
            Color = Color.Red
        };
        eb.AddField("Hibaüzenet", $"```{exception}```");
        return eb.Build();
    }
}