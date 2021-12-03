using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Victoria;

namespace KBot.Helpers
{
    public static class EmbedHelper
    {
        public static Task<Embed> MakeJoin(DiscordSocketClient client, SocketUser user, IVoiceChannel vChannel, bool failed)
        {
            return Task.Run(() =>
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = failed ? "Sikertelen csatlakozás" : "Sikeres csatlakozás",
                        IconUrl = client.CurrentUser.GetAvatarUrl()
                    },
                    Description = failed ? "Nem vagy hangcsatornában" : $"A következő csatornába: `{vChannel.Name}`",
                    Color = failed ? Color.Red : Color.Green,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | {DateTime.UtcNow}",
                        IconUrl = user.GetAvatarUrl()
                    },
                };
                return eb.Build();
            });
        }
        public static Task<Embed> MakeLeave(DiscordSocketClient client, SocketUser user, IVoiceChannel vChannel, bool failed)
        {
            return Task.Run(() =>
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = failed ? "Sikertelen elhagyás" : "Sikeres elhagyás",
                        IconUrl = client.CurrentUser.GetAvatarUrl()
                    },
                    Description = failed ? "Nem vagy hangcsatornában" : $"A következő csatornából: `{vChannel.Name}`",
                    Color = failed ? Color.Red : Color.Green,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | {DateTime.UtcNow}",
                        IconUrl = user.GetAvatarUrl()
                    },
                };
                return eb.Build();
            });
        }
        public static Task<Embed> MakeMove(DiscordSocketClient client, SocketUser user, LavaPlayer player, IVoiceChannel vChannel, bool failed)
        {
            return Task.Run(() =>
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = failed ? "Sikertelen mozgatás" : "Sikeres mozgatás",
                        IconUrl = client.CurrentUser.GetAvatarUrl()
                    }, 
                    Description = failed ? "Nem vagy hangcsatornában" : $"A következő csatornába: `{vChannel.Name}`",
                    Color = failed ? Color.Red : Color.Green,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | {DateTime.UtcNow}",
                        IconUrl = user.GetAvatarUrl()
                    },
                };
                if (player != null)
                {
                    eb.AddField(x =>
                    {
                        x.Name = "Most játszott";
                        x.Value = $"`{player.Track.Title}`";
                        x.IsInline = true;
                    });
                    eb.AddField(x =>
                    {
                        x.Name = "Következő";
                        x.Value = $"`{player.Queue.Peek().Title}`";
                        x.IsInline = true;
                    });
                };
                return eb.Build();
            });
        }
        public static Task<Embed> MakePlay(DiscordSocketClient client, SocketUser user, IVoiceChannel vChannel, LavaTrack track, LavaPlayer player, bool noMatches, bool queued)
        {
            return Task.Run(() =>
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = noMatches ? "Nincs találat" : "A következő lejátszása",
                        IconUrl = client.CurrentUser.GetAvatarUrl()
                    },
                    Title = track.Title,
                    Url = track.Url,
                    Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                    Color = noMatches ? Color.Red : Color.Green,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | {track.Duration}",
                        IconUrl = user.GetAvatarUrl()
                    },
                };
                if (!queued) return eb.Build();
                eb.WithAuthor("Hozzáadva a várolistához");
                eb.WithColor(Color.Orange);
                return eb.Build();
            });
        }
        public static Task<Embed> MakeStop(DiscordSocketClient client, SocketUser user, LavaPlayer player, bool failed)
        {
            return Task.Run(() =>
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = failed ? "Sikertelen megállítás" : "Lejátszás megállítva",
                        IconUrl = client.CurrentUser.GetAvatarUrl()
                    },
                    Description = failed ? "Jelenleg nincs zene lejátszás alatt" : $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                    Color = failed ? Color.Red : Color.Green,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | {DateTime.UtcNow}",
                        IconUrl = user.GetAvatarUrl()
                    },
                };
                return eb.Build();
            });
        }
        public static Task<Embed> MakeSkip(DiscordSocketClient client, SocketUser user, LavaPlayer player, bool failed)
        {
            return Task.Run(() =>
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = failed ? "Hiba az átugrás során" : "Sikeres átugrás",
                        IconUrl = client.CurrentUser.GetAvatarUrl()
                    },
                    Description = failed ? "Jelenleg nincs zene lejátszás alatt vagy a várólista üres" : $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                    Color = failed ? Color.Red : Color.Green,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | {DateTime.UtcNow}",
                        IconUrl = user.GetAvatarUrl()
                    },
                };
                if (player != null)
                {
                    eb.WithTitle(player.Track.Title);
                    eb.WithUrl(player.Track.Url);
                };
                return eb.Build();
            });
        }
        public static Task<Embed> MakePauseOrResume(DiscordSocketClient client, SocketUser user, LavaPlayer player, bool failed, bool resumed)
        {
            return Task.Run(() =>
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = failed ? "Hiba a szünet/folytatás során" : resumed ? "Szüneteltetés" : "Folytatás",
                        IconUrl = client.CurrentUser.GetAvatarUrl()
                    },
                    Description = failed ? "Jelenleg nincs zene lejátszás alatt" : $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                    Color = failed ? Color.Red : Color.Green,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | {DateTime.UtcNow}",
                        IconUrl = user.GetAvatarUrl()
                    },
                };
                if (player != null)
                {
                    eb.WithTitle(player.Track.Title);
                    eb.WithUrl(player.Track.Url);
                };
                return eb.Build();
            });
        }
        public static Task<Embed> MakeVolume(DiscordSocketClient client, SocketUser user, LavaPlayer player, int volume, bool failed)
        {
            return Task.Run(() =>
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = failed ? "Hiba a hangerő beállításakor" : $"Hangerő {volume}%-ra állítva beállítva",
                        IconUrl = client.CurrentUser.GetAvatarUrl()
                    },
                    Description = failed ? "Jelenleg nincs zene lejátszás alatt" : $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                    Color = failed ? Color.Red : Color.Green,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | {DateTime.UtcNow}",
                        IconUrl = user.GetAvatarUrl()
                    },
                };
                if (player != null && volume >= 0 & volume <= 100)
                {
                    eb.WithDescription("A hangerőnek 0 és 100 között kell lennie");
                };
                if (player != null)
                {
                    eb.WithTitle(player.Track.Title);
                    eb.WithUrl(player.Track.Url);
                };
                return eb.Build();
            });
        }
        public static Task<Embed> MakeFastForward(DiscordSocketClient client, SocketUser user, LavaPlayer player, TimeSpan time, bool failed)
        {
            return Task.Run(() =>
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = failed ? "Hiba a zene előretekerésekor" : $"Zene előretekerve ide {time}",
                        IconUrl = client.CurrentUser.GetAvatarUrl()
                    },
                    Description = failed ? "Jelenleg nincs zene lejátszás alatt" : $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                    Color = failed ? Color.Red : Color.Green,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | {DateTime.UtcNow}",
                        IconUrl = user.GetAvatarUrl()
                    },
                };
                if (player != null)
                {
                    eb.WithTitle(player.Track.Title);
                    eb.WithUrl(player.Track.Url);
                };
                return eb.Build();
            });
        }
    }
}