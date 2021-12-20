using System;
using System.Collections.Generic;
using System.Linq;
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
    public static Task<Embed> MakeJoin(SocketUser user, IVoiceChannel vChannel, bool failed)
    {
        return Task.Run(() =>
        {
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = failed ? "SIKERTELEN CSATLAKOZÁS" : "SIKERES CSATLAKOZÁS",
                    IconUrl = user.GetAvatarUrl()
                },
                Description = failed ? "Nem vagy hangcsatornában" : $"A következő csatornába: `{vChannel.Name}`",
                Color = failed ? Color.Red : Color.Green,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Kérte -> {user.Username}"
                }
            };
            return eb.Build();
        });
    }

    public static Task<Embed> MakeLeave(SocketUser user, IVoiceChannel vChannel, bool failed)
    {
        return Task.Run(() =>
        {
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = failed ? "SIKERTELEN ELHAGYÁS" : "SIKERES ELHAGYÁS",
                    IconUrl = user.GetAvatarUrl()
                },
                Description = failed ? "Nem vagy hangcsatornában" : $"A következő csatornából: `{vChannel.Name}`",
                Color = failed ? Color.Red : Color.Green,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Kérte -> {user.Username}"
                }
            };
            return eb.Build();
        });
    }

    public static Task<Embed> MakeMove(SocketUser user, LavaPlayer player, IVoiceChannel vChannel, bool failed)
    {
        return Task.Run(() =>
        {
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = failed ? "SIKERTELEN MOZGATÁS" : "SIKERES MOZGATÁS",
                    IconUrl = user.GetAvatarUrl()
                },
                Description = failed ? "Nem vagy hangcsatornában" : $"A következő csatornába: `{vChannel.Name}`",
                Color = failed ? Color.Red : Color.Green,
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
                        Name = "😃 Kérte",
                        Value = $"{user.Mention}",
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
                        Value = isloopEnabled ? "`Igen`" : "`Nem`",
                        IsInline = true
                    },
                    new()
                    {
                        Name = "🔊 Hangerő",
                        Value = $"`{volume}%`",
                        IsInline = true
                    }
                }
                /*Footer = new EmbedFooterBuilder
                {
                    Text = $"Kérte -> {user.Username} | Hosszúság -> {player.Track.Duration:hh\\:mm\\:ss}"
                }*/
            };
            return eb.Build();
        });
    }

    public static Task<Embed> MakeStop(SocketUser user, LavaPlayer player, bool failed)
    {
        return Task.Run(() =>
        {
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = failed ? "SIKERTELEN MEGÁLLÍTÁS" : "LEJÁTSZÁS MEGÁLLÍTVA",
                    IconUrl = user.GetAvatarUrl()
                },
                Description = failed
                    ? "Jelenleg nincs zene lejátszás alatt"
                    : $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                Color = failed ? Color.Red : Color.Green,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Kérte -> {user.Username}"
                }
            };
            return eb.Build();
        });
    }

    public static Task<Embed> MakeSkip(SocketUser user, LavaPlayer player, string thumbnailUrl, bool failed)
    {
        return Task.Run(() =>
        {
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = failed ? "SIKERTELEN ÁTUGRÁS" : "SIKERES ÁTUGRÁS",
                    IconUrl = user.GetAvatarUrl()
                },
                Description = failed
                    ? "Jelenleg nincs zene lejátszás alatt vagy a várólista üres"
                    : $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                Color = failed ? Color.Red : Color.Green,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Kérte -> {user.Username} | Hossz -> {player.Track.Duration:hh\\:mm\\:ss}"
                }
            };
            if (player == null) return eb.Build();
            eb.WithTitle(player.Track.Title);
            eb.WithUrl(player.Track.Url);
            eb.WithImageUrl(thumbnailUrl);

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

    public static Task<Embed> MakeVolume(SocketUser user, LavaPlayer player, int volume, bool failed)
    {
        return Task.Run(() =>
        {
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = failed ? "SIKERTELEN HANGERŐÁLLÍTÁS" : $"HANGERŐ {volume}%-RA ÁLLÍTVA",
                    IconUrl = user.GetAvatarUrl()
                },
                Description = failed
                    ? "Jelenleg nincs zene lejátszás alatt"
                    : $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                Color = failed ? Color.Red : Color.Green,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Kérte -> {user.Username}"
                }
            };
            if (player == null) return eb.Build();
            eb.WithTitle(player.Track.Title);
            eb.WithUrl(player.Track.Url);

            return eb.Build();
        });
    }

    public static Task<Embed> MakeFilter(SocketUser user, LavaPlayer player,
        string filtername, bool failed)
    {
        return Task.Run(() =>
        {
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = failed ? "HIBA A FILTER BEÁLLÍTÁSAKOR" : $"FILTER AKTIVÁLVA: {filtername}",
                    IconUrl = user.GetAvatarUrl()
                },
                Description = failed
                    ? "Jelenleg nincs zene lejátszás alatt"
                    : $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                Color = failed ? Color.Red : Color.Green,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Kérte -> {user.Username}"
                }
            };
            if (player == null) return eb.Build();
            eb.WithTitle(player.Track.Title);
            eb.WithUrl(player.Track.Url);
            eb.WithFooter($"Kérte -> {user.Username} | Hossz-> {player.Track.Duration:hh\\:mm\\:ss}");
            return eb.Build();
        });
    }

    public static Task<Embed> MakeLoop(SocketUser user, LavaPlayer player, bool failed)
    {
        return Task.Run(() =>
        {
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = failed ? "SIKERTELEN ISMÉTLÉS" : "ZENE ISMÉTLÉSE",
                    IconUrl = user.GetAvatarUrl()
                },
                Description = failed
                    ? "Jelenleg nincs zene lejátszás alatt"
                    : $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                Color = failed ? Color.Red : Color.Green,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Kérte -> {user.Username}"
                }
            };
            if (player == null) return eb.Build();
            eb.WithTitle(player.Track.Title);
            eb.WithUrl(player.Track.Url);
            return eb.Build();
        });
    }

    public static Task<Embed> MakeQueue(SocketUser user, LavaPlayer player, bool failed, bool cleared = false)
    {
        return Task.Run(() =>
        {
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = failed ? "SIKERTELEN LEKÉRÉS" :
                        cleared ? "LEJÁTSZÁSI LISTA TÖRÖLVE" : "LEJÁTSZÁSI LISTA LEKÉRVE",
                    IconUrl = user.GetAvatarUrl()
                },
                Description = failed
                    ? "Jelenleg nincs zene lejátszás alatt"
                    : $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                Color = failed ? Color.Red : Color.Green,
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
                Color = Color.Green,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Kérte -> {user.Username} | Hosszúság -> {player.Track.Duration:hh\\:mm\\:ss}"
                }
            };
            eb.WithColor(Color.Orange);
            return eb.Build();
        });
    }
}