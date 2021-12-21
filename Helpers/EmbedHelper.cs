using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Victoria;
using Victoria.Enums;

namespace KBot.Helpers;

public static class EmbedHelper
{
    public static Task<Embed> MakeJoin(SocketUser user, IVoiceChannel vChannel)
    {
        return Task.Run(() =>
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
            };
            return eb.Build();
        });
    }

    public static Task<Embed> MakeLeave(SocketUser user, IVoiceChannel vChannel)
    {
        return Task.Run(() =>
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
            };
            return eb.Build();
        });
    }

    public static Task<Embed> MakeMove(SocketUser user, LavaPlayer player, IVoiceChannel vChannel)
    {
        return Task.Run(() =>
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
            };
            if (player.PlayerState != PlayerState.Playing) return eb.Build();
            eb.AddField(x =>
            {
                x.Name = "MOST JÁTSZOTT";
                x.Value = $"`{player.Track.Title}`";
                x.IsInline = true;
            });
            eb.WithFooter(
                $"Kérte -> {user.Username} | " +
                $"Hossz -> {player.Track.Duration:hh\\:mm\\:ss} | " +
                "Hely a ");
            if (player.Queue.Count < 1) return eb.Build();
            eb.AddField(x =>
            {
                x.Name = "KÖVETKEZŐ";
                x.Value = $"`{player.Queue.Peek().Title}`";
                x.IsInline = true;
            });
            eb.WithFooter(
                $"Kérte -> {user.Username} | Hossz -> {player.Track.Duration:hh\\:mm\\:ss}");
            return eb.Build();
        });
    }

    public static Task<Embed> MakeNowPlaying(SocketUser user, LavaTrack track, LavaPlayer player, string thumbnailUrl, bool isloopEnabled, int volume)
    {
        return Task.Run(() =>
        {
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = "MOST JÁTSZOTT",
                    IconUrl = user.GetAvatarUrl()
                },
                Title = track.Title,
                Url = track.Url,
                ImageUrl = thumbnailUrl,
                Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
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
                        Name = "🎙️ Csatorna",
                        Value = $"`{player.VoiceChannel.Name}`",
                        IsInline = true
                    },
                    new()
                    {
                        Name = "🕐 Hosszúság",
                        Value = $"`{track.Duration:hh\\:mm\\:ss}`",
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
                    }
                },
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Dátum: {DateTime.Now:yyyy.MM.dd}"
                }
            };
            return eb.Build();
        });
    }

    public static Task<Embed> MakeStop(SocketUser user, LavaPlayer player)
    {
        return Task.Run(() =>
        {
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = "LEJÁTSZÁS MEGÁLLÍTVA",
                    IconUrl = user.GetAvatarUrl()
                },
                Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                Color = Color.Green,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Kérte -> {user.Username}"
                }
            };
            return eb.Build();
        });
    }

    public static Task<Embed> MakeSkip(SocketUser user, LavaPlayer player, string thumbnailUrl)
    {
        return Task.Run(() =>
        {
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = "ZENE ÁTUGORVA",
                    IconUrl = user.GetAvatarUrl()
                },
                Title = player.Track.Title,
                Url = player.Track.Url,
                ImageUrl = thumbnailUrl,
                Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                Color = Color.Green,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Kérte -> {user.Username} | Hossz -> {player.Track.Duration:hh\\:mm\\:ss}"
                }
            };
            return eb.Build();
        });
    }

    public static Task<Embed> MakePauseOrResume(SocketUser user, LavaPlayer player, string thumbnailUrl, bool resumed,
        bool loop)
    {
        return Task.Run(() =>
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
                ImageUrl = thumbnailUrl,
                Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                Color = Color.Green,
                Fields = new List<EmbedFieldBuilder>
                {
                    new()
                    {
                        Name = "Kérte",
                        Value = $"{user.Mention}",
                        IsInline = true
                    },
                    new()
                    {
                        Name = "Hosszúság",
                        Value = $"`{player.Track.Duration:hh\\:mm\\:ss}`",
                        IsInline = true
                    },
                    new()
                    {
                        Name = "Ismétlés",
                        Value = loop ? "`Igen`" : "`Nem`",
                        IsInline = true
                    },
                    new()
                    {
                        Name = "Hangerő",
                        Value = $"`{player.Volume}%`",
                        IsInline = true
                    }
                },
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Kérte -> {user.Username} | Hosszúság -> {player.Track.Duration:hh\\:mm\\:ss}"
                }
            };
            return eb.Build();
        });
    }

    public static Task<Embed> MakeVolume(SocketUser user, LavaPlayer player, int volume)
    {
        return Task.Run(() =>
        {
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = $"HANGERŐ {volume}%-RA ÁLLÍTVA",
                    IconUrl = user.GetAvatarUrl()
                },
                Title = player.Track.Title,
                Url = player.Track.Url,
                Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                Color = Color.Green,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Kérte -> {user.Username}"
                }
            };
            return eb.Build();
        });
    }

    public static Task<Embed> MakeFilter(SocketUser user, LavaPlayer player,
        string filtername)
    {
        return Task.Run(() =>
        {
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = $"FILTER AKTIVÁLVA: {filtername}",
                    IconUrl = user.GetAvatarUrl()
                },
                Title = player.Track.Title,
                Url = player.Track.Url,
                Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                Color = Color.Green,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Kérte -> {user.Username}"
                }
            };
            return eb.Build();
        });
    }

    public static Task<Embed> MakeLoop(SocketUser user, LavaPlayer player)
    {
        return Task.Run(() =>
        {
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = "ZENE ISMÉTLÉSE AKTIVÁLVA",
                    IconUrl = user.GetAvatarUrl()
                },
                Title = player.Track.Title,
                Url = player.Track.Url,
                Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                Color = Color.Green,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Kérte -> {user.Username}"
                }
            };
            return eb.Build();
        });
    }

    public static Task<Embed> MakeQueue(SocketUser user, LavaPlayer player, bool cleared = false)
    {
        return Task.Run(() =>
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
            if (cleared) return eb.Build();
            if (player.Queue.Count == 0) eb.WithDescription("`Nincs zene a lejátszási listában`");
            var desc = new StringBuilder();
            foreach (var track in player.Queue)
                desc.AppendLine(
                    $":{(player.Queue.TakeWhile(n => n != track).Count() + 1).ToWords()}: [`{track.Title}`]({track.Url}) | Hossz: {track.Duration:hh\\:mm\\:ss}" +
                    "\n");

            eb.WithDescription(desc.ToString());
            return eb.Build();
        });
    }

    public static Task<Embed> MakeAddedToQueue(SocketUser user, LavaTrack track, LavaPlayer player, string thumbnailUrl)
    {
        return Task.Run(() =>
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
                ImageUrl = thumbnailUrl,
                Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                Color = Color.Orange,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Kérte -> {user.Username} | Hosszúság -> {player.Track.Duration:hh\\:mm\\:ss}"
                }
            };
            return eb.Build();
        });
    }

    public static Task<Embed> MakeError(SocketUser user, string exception)
    {
        return Task.Run(() =>
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
            return eb.Build();
        });
    }
}