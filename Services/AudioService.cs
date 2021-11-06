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
using KBot.Helpers;

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
            _lavaNode.OnTrackEnded += OnTrackEnded;
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
                return await EmbedHelper.MakeEmbed(_client, null, user, $"Hiba a csatlakozáskor", $"Már csatlakozva vagyok ide: `{_lavaNode.GetPlayer(guild).VoiceChannel.Name}`", Color.Green);
            }

            if (vChannel is null)
            {
                return await EmbedHelper.MakeEmbed(_client, null, user, $"Hiba a csatlakozáskor", $"Nem vagy hangcsatornában", Color.Green);
            }
            await _lavaNode.JoinAsync(vChannel, tChannel);
            return await EmbedHelper.MakeEmbed(_client, null, user, $"Sikeres csatlakozás", $"A következő csatornába: `{vChannel.Name}`", Color.Red);
        }

        public async Task<Embed> LeaveAsync(IVoiceChannel vChannel, SocketUser user)
        {
            await _lavaNode.LeaveAsync(vChannel);
            return await EmbedHelper.MakeEmbed(_client, null, user, $"Hangcsatorna elhagyva", $"`{vChannel.Name}`", Color.Green);
        }

        public async Task<Embed> MoveAsync(IGuild guild, IVoiceChannel vChannel, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);
            await _lavaNode.MoveChannelAsync(vChannel);
            return await EmbedHelper.MakeEmbed(_client, player, user, $"Átlépve másik hangcsatornába", $"Innen : `{player.VoiceChannel.Name}` \n Ide: `{vChannel.Name}`", Color.Red);
        }
        
        public async Task<Embed> PlayAsync([Remainder] string query, IGuild guild, IVoiceChannel vChannel, ITextChannel tChannel, SocketUser user)
        {
            LavaTrack track;
            var search = Uri.IsWellFormedUriString(query, UriKind.Absolute) ?
                    await _lavaNode.SearchAsync(SearchType.Direct, query) : await _lavaNode.SearchYouTubeAsync(query);
            track = search.Tracks.FirstOrDefault();
            var player = _lavaNode.HasPlayer(guild) ? _lavaNode.GetPlayer(guild) : await _lavaNode.JoinAsync(vChannel, tChannel);

            if (search.Status == SearchStatus.NoMatches)
            {
                return await EmbedHelper.MakeEmbed(_client, player, user, $"Hiba a kereséskor", $"Nincs találat a következőre: `{query}`", Color.Green);
            }

            if (player.Track != null && player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
            {
                player.Queue.Enqueue(track);
                return await EmbedHelper.MakeEmbed(_client, player, user, $"Hozzáadva a várolistához", $"Ebben a csatornában: `{vChannel.Name}`", Color.Red);
            }
            else
            {
                await player.PlayAsync(track);
                return await EmbedHelper.MakeEmbed(_client, player, user, $"Most Játszott", $"Ebben a csatornában: `{vChannel.Name}`", Color.Red);
            }
        }
        public async Task<Embed> StopAsync(IGuild guild, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player == null)
            {
                return await EmbedHelper.MakeEmbed(_client, player, user, $"Hiba a lejátszás megállításakor", $"Jelenleg nem játszok le semmit", Color.Green);
            }

            await player.StopAsync();
            return await EmbedHelper.MakeEmbed(_client, player, user, $"Lejátszás megállítva", $"Ebben a csatornában: `{player.VoiceChannel.Name}`", Color.Red);
        }
        public async Task<Embed> SkipAsync(IGuild guild, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player == null || player.Queue.Count() == 0)
            {
                return await EmbedHelper.MakeEmbed(_client, player, user, $"Hiba az átugrás során", $"Ebben a csatornában: `{player.VoiceChannel.Name}` \n A várolista üres", Color.Green);
            }  
            await player.SkipAsync();
            return await EmbedHelper.MakeEmbed(_client, player, user, $"Most Játszott", $"Ebben a csatornában: `{player.VoiceChannel.Name}`", Color.Red);
        }

        public async Task<Embed> PauseOrResumeAsync(IGuild guild, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);

            if (player == null)
            {
                return await EmbedHelper.MakeEmbed(_client, player, user, $"Hiba a lejátszás megállításakor", $"Jelenleg nem játszok le semmit", Color.Green);
            }

            if (player.PlayerState == PlayerState.Playing)
            {
                await player.PauseAsync();
                return await EmbedHelper.MakeEmbed(_client, player, user, $"Lejátszás szüneteltetve", $"Ebben a csatornában: `{player.VoiceChannel.Name}`", Color.Red);
            }
            else
            {
                await player.ResumeAsync();
                return await EmbedHelper.MakeEmbed(_client, player, user, $"Lejátszás folytatása", $"Ebben a csatornában: `{player.VoiceChannel.Name}`", Color.Red);
            }
        }

        public async Task<Embed> SetVolume(ushort volume, IGuild guild, Int64 uid, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player == null)
            {
                return await EmbedHelper.MakeEmbed(_client, player, user, $"Hiba a hangerő állításakor", $"`Jelenleg nem játszok le zenét`", Color.Green);
            }
            if (uid != 132797923049209856)
            {
                if (volume >= 0 & volume <= 100)
                {
                    await player.UpdateVolumeAsync(volume);
                    return await EmbedHelper.MakeEmbed(_client, player, user, $"Hangerő {volume}%-ra állítva", $"Ebben a csatornában: `{player.VoiceChannel.Name}`", Color.Red);
                }
                else
                {
                    return await EmbedHelper.MakeEmbed(_client, player, user, $"Hiba a hangerő állításakor", $"A hangerőnek 0 és 100 között kell lennie \n Ebben a csatornában: `{player.VoiceChannel.Name}`", Color.Green);
                }
            }
            else
            {
                await player.UpdateVolumeAsync(volume);
                return await EmbedHelper.MakeEmbed(_client, player, user, $"Hangerő {volume}%-ra állítva", $"Ebben a csatornában: `{player.VoiceChannel.Name}`", Color.Red);
            }
        }

        public async Task<Embed> SetBassBoost(IGuild guild, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);

            EqualizerBand[] eq = 
            {
                new EqualizerBand(0, -0.075),
                new EqualizerBand(1, .125),
                new EqualizerBand(2, .125),
                new EqualizerBand(3, .1),
                new EqualizerBand(4, .1),
                //new EqualizerBand(5, .05),
                //new EqualizerBand(6, 0.075),
                //new EqualizerBand(7, .001),
                //new EqualizerBand(8, .001),
                //new EqualizerBand(9, .001),
                //new EqualizerBand(10, .001),
                //new EqualizerBand(11, .001),
                //new EqualizerBand(12, .125),
                //new EqualizerBand(13, .15),
                //new EqualizerBand(14, .05),

            };
            await player.EqualizerAsync(eq);
            return await EmbedHelper.MakeEmbed(_client, player, user, $"Filter aktiválva: Bass Boost", $"Ebben a csatornában: `{player.VoiceChannel.Name}`", Color.Red);
        }

        public async Task<Embed> FastForward(TimeSpan time, IGuild guild, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player == null)
            {
                return await EmbedHelper.MakeEmbed(_client, player, user, $"Hiba a zene előretekerésekor", $"`Jelenleg nem játszok le zenét`", Color.Green);
            }
            else
            {
                await player.SeekAsync(time);
                return await EmbedHelper.MakeEmbed(_client, player, user, $"Zene előretekerve ide {time}", $"Ebben a csatornában: `{player.VoiceChannel.Name}`", Color.Red);

            }
        }

        private async Task OnReadyAsync()
        {
            await _lavaNode.ConnectAsync();
        }

        public static bool ShouldPlayNext(TrackEndReason trackEndReason)
        {
            return trackEndReason == TrackEndReason.Finished || trackEndReason == TrackEndReason.LoadFailed;
        }
        private async Task OnTrackEnded(TrackEndedEventArgs args)
        {
            if (!ShouldPlayNext(args.Reason))
            {
                return;
            }

            var player = args.Player;
            if (!player.Queue.TryDequeue(out var queueable))
            {
                //await player.TextChannel.SendMessageAsync("Queue completed! Please add more tracks to rock n' roll!");
                return;
            }

            if (!(queueable is LavaTrack track))
            {
                //await player.TextChannel.SendMessageAsync("Next item in queue is not a track.");
                return;
            }

            await args.Player.PlayAsync(track);
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = "Most Játszott",
                    IconUrl = _client.CurrentUser.GetAvatarUrl()
                },
                Color = Color.Green,
                Description = $"Ebben a csatornában: `{player.VoiceChannel.Name}`",
                Title = track.Title,
                Url = track.Url,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"KBot {DateTime.UtcNow}",
                    IconUrl = _client.CurrentUser.GetAvatarUrl()
                }
            };
            await player.TextChannel.SendMessageAsync(string.Empty, false, eb.Build());

        }
    }
}
