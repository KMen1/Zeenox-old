using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Filters;
using Victoria.Responses.Search;

namespace KBot.Services
{
    public class AudioService
    {
        private readonly LavaNode _lavaNode;
        private readonly DiscordSocketClient _client;

        public AudioService(DiscordSocketClient client, LavaNode lavaNode)
        {
            _lavaNode = lavaNode;
            _client = client;
        }
        public Task InitializeAsync()
        {
            _client.Ready += OnReadyAsync; 
            _lavaNode.OnTrackEnded += OnTrackFinished;
            _lavaNode.OnTrackException += _lavaNode_OnTrackException;
            
            return Task.CompletedTask;
        }
        private async Task _lavaNode_OnTrackException(TrackExceptionEventArgs arg)
        {
            await arg.Player.StopAsync();        
        }
        public async Task<Embed> JoinAsync(IGuild guild, IVoiceChannel vChannel, ITextChannel tChannel, SocketUser user)
        {
            if (_lavaNode.HasPlayer(guild))
            {
                var heb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = "Hiba a csatlakozáskor",
                        IconUrl = _client.CurrentUser.GetAvatarUrl()
                    },
                    Color = Color.Red,
                    Description = $"Már csatlakozva vagyok ide: `{_lavaNode.GetPlayer(guild).VoiceChannel.Name}`",
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | KBot 2021",
                        IconUrl = user.GetAvatarUrl()
                    }
                };
                return heb.Build();
            }

            if (vChannel is null)
            {
                var neb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = "Hiba a csatlakozáskor",
                        IconUrl = _client.CurrentUser.GetAvatarUrl()
                    },
                    Color = Color.Red,
                    Description = $"Nem vagy hangcsatornában",
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | KBot 2021",
                        IconUrl = user.GetAvatarUrl()
                    }
                };
                return neb.Build();
            }
            await _lavaNode.JoinAsync(vChannel, tChannel);
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = "Sikeres csatlakozás",
                    IconUrl = _client.CurrentUser.GetAvatarUrl()
                },
                Color = Color.Red,
                Description = $"A következő csatornába: `{vChannel.Name}`",
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{user.Username} | KBot 2021",
                    IconUrl = user.GetAvatarUrl()
                }
            };
            return eb.Build();
        }

        public async Task<Embed> LeaveAsync(IVoiceChannel vChannel, SocketUser user)
        {
            await _lavaNode.LeaveAsync(vChannel);
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = "Hangcsatorna elhagyva",
                    IconUrl = _client.CurrentUser.GetAvatarUrl()
                },
                Color = Color.Red,
                Description = $"`{vChannel.Name}`",
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{user.Username} | KBot 2021",
                    IconUrl = user.GetAvatarUrl()
                }
            };
            return eb.Build();
        }

        public async Task<Embed> MoveAsync(IGuild guild, IVoiceChannel vChannel, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);
            await _lavaNode.MoveChannelAsync(vChannel);
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = "Átlépve másik hangcsatornába",
                    IconUrl = _client.CurrentUser.GetAvatarUrl()
                },
                Color = Color.Blue,
                Description = $"Innen : `{player.VoiceChannel.Name}` \n Ide: `{vChannel.Name}`",
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{user.Username} | KBot 2021",
                    IconUrl = user.GetAvatarUrl()
                }
            };
            return eb.Build();
        }
        
        public async Task<Embed> PlayAsync([Remainder] string query, IGuild guild, IVoiceChannel vChannel, ITextChannel tChannel, SocketUser user)
        {
            LavaTrack track;
            var search = Uri.IsWellFormedUriString(query, UriKind.Absolute) ?
                    await _lavaNode.SearchAsync(SearchType.Direct, query) : await _lavaNode.SearchYouTubeAsync(query);
            track = search.Tracks.FirstOrDefault();
            if (search.Status == SearchStatus.NoMatches)
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = "Hiba a kereséskor",
                        IconUrl = _client.CurrentUser.GetAvatarUrl()
                    },
                    Color = Color.Red,
                    Description = $"Nincs találat a következőre: `{query}`",
                    Title = track.Title,
                    Url = track.Url,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | KBot 2021",
                        IconUrl = user.GetAvatarUrl()
                    }
                };
                return eb.Build();
            }

            var player = _lavaNode.HasPlayer(guild) ? _lavaNode.GetPlayer(guild) : await _lavaNode.JoinAsync(vChannel, tChannel);

            if (player.Track != null && player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
            {
                player.Queue.Enqueue(track);
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = "Hozzáadva a várolistához",
                        IconUrl = _client.CurrentUser.GetAvatarUrl()
                    },
                    Color = Color.Orange,
                    Description = $"Ebben a csatornában: `{vChannel.Name}`",
                    Title = track.Title,
                    Url = track.Url,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | KBot 2021",
                        IconUrl = user.GetAvatarUrl()
                    }
                };
                return eb.Build();
            }
            else
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = "Most Játszott",
                        IconUrl = _client.CurrentUser.GetAvatarUrl()
                    },
                    Color = Color.Green,
                    Description = $"Ebben a csatornában: `{vChannel.Name}`",
                    Title = track.Title,
                    Url = track.Url,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | KBot 2021",
                        IconUrl = user.GetAvatarUrl()
                    }
                };
                await player.PlayAsync(track);
                return eb.Build();
            }
        }
        public async Task<Embed> StopAsync(IGuild guild, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player == null)
            {
                var eeb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = "Hiba a lejátszás megállításakor",
                        IconUrl = _client.CurrentUser.GetAvatarUrl()
                    },
                    Color = Color.Red,
                    Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}` \n Jelenleg nem játszok le semmit",
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | KBot 2021",
                        IconUrl = user.GetAvatarUrl()
                    }
                };
                return eeb.Build();

            }

            await player.StopAsync();
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = "Lejátszás megállítva",
                    IconUrl = _client.CurrentUser.GetAvatarUrl()
                },
                Color = Color.Red,
                Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                Title = player.Track.Title,
                Url = player.Track.Url,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{user.Username} | KBot 2021",
                    IconUrl = user.GetAvatarUrl()
                }
            };
            return eb.Build();
        }
        public async Task<Embed> SkipAsync(IGuild guild, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player == null || player.Queue.Count() == 0)
            {
                var gb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = "Hiba az átugrás során",
                        IconUrl = _client.CurrentUser.GetAvatarUrl()
                    },
                    Color = Color.Red,
                    Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}` \n A várolista üres",
                    Title = player.Track.Title,
                    Url = player.Track.Url,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | KBot 2021",
                        IconUrl = user.GetAvatarUrl()
                    }
                };
                return gb.Build();
            }  

            await player.SkipAsync();
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = "Most Játszott",
                    IconUrl = _client.CurrentUser.GetAvatarUrl()
                },
                Color = Color.Green,
                Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                Title = player.Track.Title,
                Url = player.Track.Url,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{user.Username} | KBot 2021",
                    IconUrl = user.GetAvatarUrl()
                }
            };
            return eb.Build();
        }

        public async Task<Embed> PauseOrResumeAsync(IGuild guild, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);

            if (player == null)
            {
                var eeb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = "Hiba a lejátszás megállításakor",
                        IconUrl = _client.CurrentUser.GetAvatarUrl()
                    },
                    Color = Color.Red,
                    Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}` \n Jelenleg nem játszok le semmit",
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | KBot 2021",
                        IconUrl = user.GetAvatarUrl()
                    }
                };
                return eeb.Build();
            }

            if (player.PlayerState == PlayerState.Playing)
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = "Lejátszás szüneteltetve",
                        IconUrl = _client.CurrentUser.GetAvatarUrl()
                    },
                    Color = Color.Red,
                    Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                    Title = player.Track.Title,
                    Url = player.Track.Url,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | KBot 2021",
                        IconUrl = user.GetAvatarUrl()
                    }
                };
                await player.PauseAsync();
                return eb.Build();
            }
            else
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = "Lejátszás folytatása",
                        IconUrl = _client.CurrentUser.GetAvatarUrl()
                    },
                    Color = Color.Green,
                    Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                    Title = player.Track.Title,
                    Url = player.Track.Url,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | KBot 2021",
                        IconUrl = user.GetAvatarUrl()
                    }
                };
                await player.ResumeAsync();
                return eb.Build();
            }
        }

        public async Task<Embed> SetVolume(ushort volume, IGuild guild, Int64 uid, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player == null)
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = $"Hiba a hangerő állításakor",
                        IconUrl = _client.CurrentUser.GetAvatarUrl()
                    },
                    Color = Color.Red,
                    Description = $"Jelenleg nem játszok le zenét`",
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | KBot 2021",
                        IconUrl = user.GetAvatarUrl()
                    }
                };
                return eb.Build();
            }
            if (uid != 132797923049209856)
            {
                if (volume >= 0 & volume <= 100)
                {
                    var eb = new EmbedBuilder
                    {
                        Author = new EmbedAuthorBuilder
                        {
                            Name = $"Hangerő {volume}%-ra állítva",
                            IconUrl = _client.CurrentUser.GetAvatarUrl()
                        },
                        Color = Color.Green,
                        Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                        Title = player.Track.Title,
                        Url = player.Track.Url,
                        Footer = new EmbedFooterBuilder
                        {
                            Text = $"{user.Username} | KBot 2021",
                            IconUrl = user.GetAvatarUrl()
                        }
                    };
                    await player.UpdateVolumeAsync(volume);
                    return eb.Build();
                }
                else
                {
                    var eb = new EmbedBuilder
                    {
                        Author = new EmbedAuthorBuilder
                        {
                            Name = $"Hiba a hangerő állításakor",
                            IconUrl = _client.CurrentUser.GetAvatarUrl()
                        },
                        Color = Color.Red,
                        Description = $"A hangerőnek 0 és 100 között kell lennie \n Ebben a csatornában: `{player.VoiceChannel.Name}`",
                        Title = player.Track.Title,
                        Url = player.Track.Url,
                        Footer = new EmbedFooterBuilder
                        {
                            Text = $"{user.Username} | KBot 2021",
                            IconUrl = user.GetAvatarUrl()
                        }
                    };
                    return eb.Build();
                }
            }
            else
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = $"Hangerő {volume}%-ra állítva",
                        IconUrl = _client.CurrentUser.GetAvatarUrl()
                    },
                    Color = Color.Green,
                    Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                    Title = player.Track.Title,
                    Url = player.Track.Url,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | KBot 2021",
                        IconUrl = user.GetAvatarUrl()
                    }
                };
                await player.UpdateVolumeAsync(volume);
                return eb.Build();
            }
        }

        public async Task<Embed> SetBassBoost(IGuild guild, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);
            EqualizerBand[] eq = {
                new EqualizerBand(0, 0.6),
                new EqualizerBand(1, 0.7),
                new EqualizerBand(2, 0.8),
                new EqualizerBand(3, 0.55),
                new EqualizerBand(4, 0.25),
                new EqualizerBand(5, 0),
                new EqualizerBand(6, -0.25),
                new EqualizerBand(7, -0.45),
                new EqualizerBand(8, -0.55),
                new EqualizerBand(9, -0.7),
                new EqualizerBand(10, -0.3),
                new EqualizerBand(11, -0.25),
                new EqualizerBand(12, 0),
                new EqualizerBand(13, 0),
                new EqualizerBand(14, 0)
                };
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = "Filter aktiválva: Bass Boost",
                    IconUrl = _client.CurrentUser.GetAvatarUrl()
                },
                Color = Color.Green,
                Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                Title = player.Track.Title,
                Url = player.Track.Url,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{user.Username} | KBot 2021",
                    IconUrl = user.GetAvatarUrl()
                }
            };
            await player.EqualizerAsync(eq);
            return eb.Build();
        }

        /*public async Task<string> SetFilter(IGuild guild, double boostAmount, Int64 uid)
        {
            var player = _lavaNode.GetPlayer(guild);
            var z = new TimescaleFilter();
            z.Pitch = 1;
            await player.ApplyFilterAsync(z);
        }*/

        public async Task<Embed> FastForward(TimeSpan time, IGuild guild, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player == null)
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = $"Hiba a zene előretekerésekor",
                        IconUrl = _client.CurrentUser.GetAvatarUrl()
                    },
                    Color = Color.Red,
                    Description = $"Jelenleg nem játszok le zenét`",
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | KBot 2021",
                        IconUrl = user.GetAvatarUrl()
                    }
                };
                return eb.Build();
            }
            else
            {
                var eb = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = $"Zene előretekerve ide {time}",
                        IconUrl = _client.CurrentUser.GetAvatarUrl()
                    },
                    Color = Color.Green,
                    Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                    Title = player.Track.Title,
                    Url = player.Track.Url,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{user.Username} | KBot 2021",
                        IconUrl = user.GetAvatarUrl()
                    }
                };
                await player.SeekAsync(time);
                return eb.Build();
                
            }
        }

        private async Task OnReadyAsync()
        {
            await _lavaNode.ConnectAsync();
        }

        private async Task OnTrackFinished(TrackEndedEventArgs arg)
        {
            //TrackEndReason reason = arg.Reason;
            LavaPlayer player = arg.Player;

            if (!player.Queue.TryDequeue(out var item) || !(item is LavaTrack nextTrack))
            {
                return;
            }
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = "Most Játszott",
                    IconUrl = _client.CurrentUser.GetAvatarUrl()
                },
                Color = Color.Green,
                Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                Title = nextTrack.Title,
                Url = nextTrack.Url,
                Footer = new EmbedFooterBuilder
                {
                    Text = "KBot 2021",
                    IconUrl = _client.CurrentUser.GetAvatarUrl()
                }
            };
            await player.PlayAsync(nextTrack);
            await player.TextChannel.SendMessageAsync(string.Empty, false, eb.Build());
        }
    }
}
