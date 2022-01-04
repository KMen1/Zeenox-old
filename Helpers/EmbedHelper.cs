using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Victoria;

namespace KBot.Helpers;

public static class EmbedHelper
{
    public static ValueTask<Embed> MakeJoin(SocketUser user, IVoiceChannel vChannel)
    {
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = "SIKERES CSATLAKOZÁS",
                IconUrl = user.GetAvatarUrl()
            },
            Description = $"A következő csatornába: `{vChannel.Name}`",
            Color = Color.Green,
            Footer = new EmbedFooterBuilder
            {
                Text = $"Kérte -> {user.Username}"
            }
        }.Build();
        return new ValueTask<Embed>(eb);
    }

    public static ValueTask<Embed> MakeLeave(SocketUser user, IVoiceChannel vChannel)
    {
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = "SIKERES ELHAGYÁS",
                IconUrl = user.GetAvatarUrl()
            },
            Description = $"A következő csatornából: `{vChannel.Name}`",
            Color = Color.Green,
            Footer = new EmbedFooterBuilder
            {
                Text = $"Kérte -> {user.Username}"
            }
        }.Build();
        return new ValueTask<Embed>(eb);
    }

    public static ValueTask<Embed> MakeMove(SocketUser user, LavaPlayer player, IVoiceChannel vChannel)
    {
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = "SIKERES ÁTHELYEZÉS",
                IconUrl = user.GetAvatarUrl()
            },
            Description = $"A következő csatornába: `{vChannel.Name}`",
            Color = Color.Green,
            Footer = new EmbedFooterBuilder
            {
                Text = $"Kérte -> {user.Username}"
            }
        }.Build();
        return new ValueTask<Embed>(eb);
    }

    public static async ValueTask<Embed> MakeNowPlaying(SocketUser user, LavaPlayer player, bool isloopEnabled, int volume, List<string> filters)
    {
        var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = "MOST JÁTSZOTT",
                    IconUrl = user.GetAvatarUrl()
                },
                Title = player.Track.Title,
                Url = player.Track.Url,
                ImageUrl = await player.Track.FetchArtworkAsync(),
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
                        Value = $"`{volume}%`",
                        IsInline = true
                    },
                    new()
                    {
                        Name = "📝 Szűrők",
                        Value = filters.Count > 0 ? $"`{string.Join(", ", filters)}`" : "`Nincsenek`",
                        IsInline = true
                    }
                },
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Dátum: {DateTime.Now:yyyy.MM.dd}"
                }
            }.Build();
        return await new ValueTask<Embed>(eb);
    }

    public static ValueTask<Embed> MakeVolume(SocketUser user, LavaPlayer player, int volume)
    {
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = $"HANGERŐ {volume}%-RA ÁLLÍTVA",
                IconUrl = user.GetAvatarUrl()
            },
            Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
            Color = Color.Green,
        }.Build();
        return new ValueTask<Embed>(eb);
    }

    public static ValueTask<Embed> MakeFilter(SocketUser user, string[] filters)
    {
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = filters.Length == 0 ? "SZŰRŐK DEAKTIVÁLVA": $"SZŰRŐK AKTIVÁLVA",
                IconUrl = user.GetAvatarUrl()
            },
            Description = $"`{string.Join(", ", filters)}`",
            Color = Color.Green
        }.Build();
        return new ValueTask<Embed>(eb);
    }

    public static ValueTask<Embed> MakeQueue(SocketUser user, LavaPlayer player, bool cleared = false)
    {
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = cleared ? "LEJÁTSZÁSI LISTA TÖRÖLVE" : "LEJÁTSZÁSI LISTA LEKÉRVE",
                IconUrl = user.GetAvatarUrl()
            },
            Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
            Color = Color.Green,
            Footer = new EmbedFooterBuilder
            {
                Text = $"Kérte -> {user.Username}"
            }
        };
        if (cleared) return new ValueTask<Embed>(eb.Build());
        if (player.Queue.Count == 0) eb.WithDescription("`Nincs zene a lejátszási listában`");
        var desc = new StringBuilder();
        foreach (var track in player.Queue)
            desc.AppendLine(
                $":{(player.Queue.TakeWhile(n => n != track).Count() + 1).ToWords()}: [`{track.Title}`]({track.Url}) | Hossz: {track.Duration:hh\\:mm\\:ss}" +
                "\n");

        eb.WithDescription(desc.ToString());
        return new ValueTask<Embed>(eb.Build());
    }

    public static async ValueTask<Embed> MakeAddedToQueue(SocketUser user, LavaTrack track, LavaPlayer player)
    {
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = "HOZZÁADVA A VÁRÓLISTÁHOZ",
                IconUrl = user.GetAvatarUrl()
            },
            Title = track.Title,
            Url = track.Url,
            ImageUrl = await track.FetchArtworkAsync(),
            Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
            Color = Color.Orange,
            Footer = new EmbedFooterBuilder
            {
                Text = $"Kérte -> {user.Username} | Hosszúság -> {player.Track.Duration:hh\\:mm\\:ss}"
            }
        }.Build();
        return await new ValueTask<Embed>(eb);
    }

    public static ValueTask<Embed> MakeError(SocketUser user, string exception)
    {
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = "HIBA",
                IconUrl = user.GetAvatarUrl()
            },
            Title = "😒 Hiba történt a parancs végrehajtása során",
            Description = "Kérlek próbáld meg újra! \n" +
                          "Ha a hiba továbbra is fennáll, kérlek jelezd a <@132797923049209856>-nek! \n",
            //$"A bot beragadása esetén használd a **/reset** parancsot!",
            Color = Color.Red,
            Footer = new EmbedFooterBuilder
            {
                Text = "Dátum: " + DateTime.Now.ToString("yyyy.MM.dd. HH:mm:ss")
            }
        };
        eb.AddField("Hibaüzenet", $"```{exception}```");
        return new ValueTask<Embed>(eb.Build());
    }
}