using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using KBot.Enums;
using Victoria;

namespace KBot.Helpers;

public static class EmbedHelper
{
    private const string SuccessIcon = "https://i.ibb.co/HdqsDXh/tick.png";
    private const string ErrorIcon = "https://i.ibb.co/SrZZggy/x.png";
    private const string PlayingGif = "https://bestanimations.com/media/discs/895872755cd-animated-gif-9.gif";

    public static ValueTask<Embed> LeaveEmbed(IVoiceChannel vChannel)
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
        return new ValueTask<Embed>(eb);
    }

    public static ValueTask<Embed> MoveEmbed(IVoiceChannel vChannel)
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
        return new ValueTask<Embed>(eb);
    }

    public static async ValueTask<Embed> NowPlayingEmbed(SocketUser user, LavaPlayer player, bool isloopEnabled, string filter)
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
                        Value = $"{user.Mention}",
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
                        Name = "🎶 Várólistán",
                        Value = $"`{player.Queue.Count.ToString()}`",
                        IsInline = true
                    },
                    new()
                    {
                        Name = "📝 Szűrő",
                        Value = filter is not null ? $"`{filter}`": "`Nincs`",
                        IsInline = true
                    }
                }
            }.Build();
        return await new ValueTask<Embed>(eb).ConfigureAwait(false);
    }

    public static ValueTask<Embed> VolumeEmbed(LavaPlayer player)
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
        return new ValueTask<Embed>(eb);
    }

    public static ValueTask<Embed> QueueEmbed(LavaPlayer player, bool cleared = false)
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
            return new ValueTask<Embed>(eb.Build());
        }

        if (player.Queue.Count == 0)
        {
            eb.WithDescription("`Nincs zene a lejátszási listában`");
        }
        else
        {
            var desc = new StringBuilder();
            foreach (var track in player.Queue)
            {
                desc.AppendLine(
                    $":{(player.Queue.TakeWhile(n => n != track).Count() + 1).ToWords()}: [`{track.Title}`]({track.Url}) | Hossz: {track.Duration:hh\\:mm\\:ss}" +
                    "\n");
            }

            eb.WithDescription(desc.ToString());
        }
        return new ValueTask<Embed>(eb.Build());
    }

    public static async ValueTask<Embed> AddedToQueueEmbed(LavaTrack track, LavaPlayer player)
    {
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = "HOZZÁADVA A VÁRÓLISTÁHOZ",
                IconUrl = SuccessIcon
            },
            Title = track.Title,
            Url = track.Url,
            ImageUrl = await track.FetchArtworkAsync().ConfigureAwait(false),
            Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
            Color = Color.Orange,
            Footer = new EmbedFooterBuilder
            {
                Text = $"Hosszúság -> {player.Track.Duration:hh\\:mm\\:ss}"
            }
        }.Build();
        return await new ValueTask<Embed>(eb).ConfigureAwait(false);
    }

    public static ValueTask<Embed> ErrorEmbed(string exception)
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
        return new ValueTask<Embed>(eb.Build());
    }

    public static ValueTask<Embed> MovieEventEmbed(SocketGuildEvent movieEvent, EventEmbedType embedType)
    {
        var embed = new EmbedBuilder
        {
            Title = movieEvent.Name,
            Description = movieEvent.Description,
            Timestamp = DateTimeOffset.UtcNow,
            Fields =
            {
                new EmbedFieldBuilder
                {
                    Name = "👨 Létrehozta",
                    Value = movieEvent.Creator.Mention,
                    IsInline = true
                },
                new EmbedFieldBuilder()
                {
                    Name = "🕐 Időpont",
                    Value = movieEvent.StartTime.ToString("yyyy. MM. dd. HH:mm"),
                    IsInline = true
                },
                new EmbedFieldBuilder()
                {
                    Name = "🎙 Csatorna",
                    Value = movieEvent.Channel.Name,
                    IsInline = true
                }
            }
        };
        switch (embedType)
        {
            case EventEmbedType.Scheduled:
            {
                embed.WithAuthor("ÚJ FILM ESEMÉNY ÜTEMEZVE!", SuccessIcon);
                embed.WithColor(Color.Orange);
                break;
            }
            case EventEmbedType.Updated:
            {
                embed.WithAuthor("FILM ESEMÉNY FRISSÍTVE!", SuccessIcon);
                embed.WithColor(Color.Orange);
                break;
            }
            case EventEmbedType.Started:
            {
                embed.WithAuthor("FILM ESEMÉNY KEZDŐDIK!", SuccessIcon);
                embed.WithColor(Color.Green);
                break;
            }
            case EventEmbedType.Cancelled:
            {
                embed.WithAuthor("FILM ESEMÉNY TÖRÖLVE!", ErrorIcon);
                embed.WithColor(Color.Red);
                break;
            }
        }
        return new ValueTask<Embed>(embed.Build());
    }

    public static ValueTask<Embed> TourEventEmbed(SocketGuildEvent tourEvent, EventEmbedType tourEmbedType)
    {
        var embed = new EmbedBuilder
        {
            Title = tourEvent.Name,
            Description = tourEvent.Description,
            Timestamp = DateTimeOffset.UtcNow,
            Fields =
            {
                new EmbedFieldBuilder
                {
                    Name = "👨 Létrehozta",
                    Value = tourEvent.Creator.Mention,
                    IsInline = true
                },
                new EmbedFieldBuilder()
                {
                    Name = "🕐 Időpont",
                    Value = tourEvent.StartTime.ToString("yyyy. MM. dd. HH:mm"),
                    IsInline = true
                },
                new EmbedFieldBuilder()
                {
                    Name = "⛺ Helyszín",
                    Value = tourEvent.Location,
                    IsInline = false
                }
            }
        };
        switch (tourEmbedType)
        {
            case EventEmbedType.Scheduled:
            {
                embed.WithAuthor("ÚJ TÚRA ESEMÉNY ÜTEMEZVE!", SuccessIcon);
                embed.WithColor(Color.Orange);
                break;
            }
            case EventEmbedType.Updated:
            {
                embed.WithAuthor("TÚRA ESEMÉNY FRISSÍTVE!", SuccessIcon);
                embed.WithColor(Color.Orange);
                break;
            }
            case EventEmbedType.Started:
            {
                embed.WithAuthor("TÚRA ESEMÉNY KEZDŐDIK!", SuccessIcon);
                embed.WithColor(Color.Green);
                break;
            }
            case EventEmbedType.Cancelled:
            {
                embed.WithAuthor("TÚRA ESEMÉNY TÖRÖLVE!", ErrorIcon);
                embed.WithColor(Color.Red);
                break;
            }
        }
        return new ValueTask<Embed>(embed.Build());
    }
}