using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;

namespace KBot.Helpers
{
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
                        Text = $"Kérte -> {user.Username}",
                        IconUrl = user.GetAvatarUrl()
                    },
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
                        Text = $"Kérte -> {user.Username}",
                        IconUrl = user.GetAvatarUrl()
                    },
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
                        Text = $"Kérte -> {user.Username}",
                    },
                };
                if (player.PlayerState != PlayerState.Playing) return eb.Build();
                eb.AddField(x =>
                {
                    x.Name = "MOST JÁTSZOTT";
                    x.Value = $"`{player.Track.Title}`";
                    x.IsInline = true;
                });
                eb.WithFooter(
                    text: $"Kérte -> {user.Username} | " +
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
                    text: $"Kérte -> {user.Username} | Hossz -> {player.Track.Duration:hh\\:mm\\:ss}");
                return eb.Build();
            });
        }
        public static Task<Embed> MakePlay(SocketUser user, LavaTrack track, LavaPlayer player, string thumbnailUrl, 
            bool noMatches, bool queued)
        {
            return Task.Run(() =>
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = noMatches ? "NINCS TALÁLAT" : "A KÖVETKEZŐ LEJÁTSZÁSA",
                        IconUrl = user.GetAvatarUrl()
                    },
                    Title = track.Title,
                    Url = track.Url,
                    ImageUrl = thumbnailUrl,
                    Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                    Color = noMatches ? Color.Red : Color.Green,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"Kérte -> {user.Username} | Hosszúság -> {player.Track.Duration:hh\\:mm\\:ss}",
                    },
                };
                if (!queued) return eb.Build();
                eb.WithAuthor("HOZZÁADVA A VÁRÓLISTÁHOZ", user.GetAvatarUrl());
                eb.WithColor(Color.Orange);
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
                    Description = failed ? "Jelenleg nincs zene lejátszás alatt" : $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                    Color = failed ? Color.Red : Color.Green,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"Kérte -> {user.Username}"
                    },
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
                    Description = failed ? "Jelenleg nincs zene lejátszás alatt vagy a várólista üres" : $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                    Color = failed ? Color.Red : Color.Green,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"Kérte -> {user.Username} | Hossz -> {player.Track.Duration:hh\\:mm\\:ss}"
                    },
                };
                if (player == null) return eb.Build();
                eb.WithTitle(player.Track.Title);
                eb.WithUrl(player.Track.Url);
                eb.WithImageUrl(thumbnailUrl);

                return eb.Build();
            });
        }
        public static Task<Embed> MakePauseOrResume(SocketUser user, LavaPlayer player, bool failed, bool resumed)
        {
            return Task.Run(() =>
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = failed ? "Hiba a szünet/folytatás során" : resumed ? "LEJÁTSZÁS SZÜNETELTEÉSE" : "LEJÁTSZÁS FOLYTATÁSA",
                        IconUrl = user.GetAvatarUrl()
                    },
                    Description = failed ? "Jelenleg nincs zene lejátszás alatt" : $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                    Color = failed ? Color.Red : Color.Green,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"Kérte -> {user.Username}"
                    },
                };
                if (player == null) return eb.Build();
                eb.WithTitle(player.Track.Title);
                eb.WithUrl(player.Track.Url);

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
                    Description = failed ? "Jelenleg nincs zene lejátszás alatt" : $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                    Color = failed ? Color.Red : Color.Green,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"Kérte -> {user.Username}",
                    },
                };
                if (player != null && volume >= 0 & volume <= 100)
                {
                    eb.WithDescription("A hangerőnek 0 és 100 között kell lennie");
                }

                if (player == null) return eb.Build();
                eb.WithTitle(player.Track.Title);
                eb.WithUrl(player.Track.Url);

                return eb.Build();
            });
        }
        public static Task<Embed> MakeFastForward(SocketUser user, LavaPlayer player, TimeSpan time, bool failed)
        {
            return Task.Run(() =>
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = failed ? "SIKERTELEN ELŐRETEKERÉS" : $"ZENE ELŐRETEKERVE IDE: {time}",
                        IconUrl = user.GetAvatarUrl()
                    },
                    Description = failed ? "Jelenleg nincs zene lejátszás alatt" : $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                    Color = failed ? Color.Red : Color.Green,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"Kérte -> {user.Username}",
                    },
                };
                if (player == null) return eb.Build();
                eb.WithTitle(player.Track.Title);
                eb.WithUrl(player.Track.Url);
                return eb.Build();
            });
        }

        public static Task<Embed> MakeFilter(SocketUser user, LavaPlayer player,
            string filtername, bool failed, bool enabled = false)
        {
            return Task.Run(() =>
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = failed ? "HIBA A FILTER BEÁLLÍTÁSAKOR" : 
                            enabled ? $"FILTER AKTIVÁLVA: {filtername}" : $"FILTER DEAKTIVÁLVA: {filtername}",
                        IconUrl = user.GetAvatarUrl()
                    },
                    Description = failed ? "Jelenleg nincs zene lejátszás alatt" : $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                    Color = failed ? Color.Red : Color.Green,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"Kérte -> {user.Username}",
                    },
                };
                if (player == null) return eb.Build();
                eb.WithTitle(player.Track.Title);
                eb.WithUrl(player.Track.Url);
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
                        Name = failed ? "SIKERTELEN ISMÉTLÉS" : $"ZENE ISMÉTLÉSE",
                        IconUrl = user.GetAvatarUrl()
                    },
                    Description = failed ? "Jelenleg nincs zene lejátszás alatt" : $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                    Color = failed ? Color.Red : Color.Green,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"Kérte -> {user.Username}",
                    },
                };
                if (player == null) return eb.Build();
                eb.WithTitle(player.Track.Title);
                eb.WithUrl(player.Track.Url);
                return eb.Build();
            });
        }
    }
}