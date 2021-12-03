using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Responses.Search;
using KBot.Helpers;
using Victoria.Filters;

namespace KBot.Services
{
    public class Audio
    {
        private readonly LavaNode _lavaNode;
        private readonly DiscordSocketClient _client;
        private bool bassboost = false;
        private bool nightcore = false;
        private bool eightD = false;
        private bool vaporwave = false;
        private bool loop = false;
        private bool karaoke = false;
        public Audio(DiscordSocketClient client, LavaNode lavaNode)
        {
            _lavaNode = lavaNode;
            _client = client;
        }
        public Task InitializeAsync()
        {
            _client.Ready += OnReadyAsync; 
            _lavaNode.OnTrackEnded += OnTrackEnded;
            _lavaNode.OnTrackException += OnTrackException;
            return Task.CompletedTask;
        }
        private static async Task OnTrackException(TrackExceptionEventArgs arg)
        {
            await arg.Player.StopAsync();   
            await arg.Player.PlayAsync(arg.Track);
        }
        public async Task<Embed> JoinAsync(IGuild guild, IVoiceChannel vChannel, ITextChannel tChannel, SocketUser user)
        {
            if (!_lavaNode.HasPlayer(guild) || vChannel is null)
            {
                return await EmbedHelper.MakeJoin(_client, user, vChannel, true);
            }
            await _lavaNode.JoinAsync(vChannel, tChannel);
            return await EmbedHelper.MakeJoin(_client, user, vChannel, false);
        }

        public async Task<Embed> LeaveAsync(IGuild guild, IVoiceChannel vChannel, SocketUser user)
        {
            if (!_lavaNode.HasPlayer(guild) || vChannel is null)
            {
                return await EmbedHelper.MakeLeave(_client, user, vChannel, true);
            }
            await _lavaNode.LeaveAsync(vChannel);
            return await EmbedHelper.MakeLeave(_client, user, vChannel, false);
        }

        public async Task<Embed> MoveAsync(IGuild guild, IVoiceChannel vChannel, SocketUser user)
        {
            if (!_lavaNode.HasPlayer(guild))
            {
                return await EmbedHelper.MakeMove(_client, user, _lavaNode.GetPlayer(guild), vChannel, true);
            }
            await _lavaNode.MoveChannelAsync(vChannel);
            return await EmbedHelper.MakeMove(_client, user, _lavaNode.GetPlayer(guild), vChannel, false);
        }
        
        public async Task<Embed> PlayAsync([Remainder] string query, IGuild guild, IVoiceChannel vChannel, ITextChannel tChannel, SocketUser user)
        {
            var search = Uri.IsWellFormedUriString(query, UriKind.Absolute) ?
                    await _lavaNode.SearchAsync(SearchType.Direct, query) : await _lavaNode.SearchYouTubeAsync(query);
            var track = search.Tracks.FirstOrDefault();
            var player = _lavaNode.HasPlayer(guild) ? _lavaNode.GetPlayer(guild) : await _lavaNode.JoinAsync(vChannel, tChannel);

            if (search.Status == SearchStatus.NoMatches)
            {
                return await EmbedHelper.MakePlay(_client, user, vChannel, track, player, true, false);
            }

            if (player.Track != null && player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
            {
                player.Queue.Enqueue(track);
                return await EmbedHelper.MakePlay(_client, user, vChannel, track, player, false, true);
            }
            else
            {
                await player.PlayAsync(track);
                return await EmbedHelper.MakePlay(_client, user, vChannel, track, player, false, false);
            }
        }
        public async Task<Embed> StopAsync(IGuild guild, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player == null)
            {
                return await EmbedHelper.MakeStop(_client, user, null, true);
            }
            await player.StopAsync();
            return await EmbedHelper.MakeStop(_client, user, player, false);
        }
        public async Task<Embed> SkipAsync(IGuild guild, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player == null || player.Queue.Count == 0)
            {
                return await EmbedHelper.MakeSkip(_client, user, player, true);
            }
            await player.SkipAsync();
            return await EmbedHelper.MakeSkip(_client, user, player, false);
        }

        public async Task<Embed> PauseOrResumeAsync(IGuild guild, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);

            if (player == null)
            {
                return await EmbedHelper.MakePauseOrResume(_client, user, null, true, false);
            }

            if (player.PlayerState == PlayerState.Playing)
            {
                await player.PauseAsync();
                return await EmbedHelper.MakePauseOrResume(_client, user, player, false, false);
            }
            else
            {
                await player.ResumeAsync();
                return await EmbedHelper.MakePauseOrResume(_client, user, player, false, true);
            }
        }

        public async Task<Embed> SetVolumeAsync(ushort volume, IGuild guild, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player == null)
            {
                return await EmbedHelper.MakeVolume(_client, user, null, volume, true);
            }

            if (user.Id == 132797923049209856)
            {
                await player.UpdateVolumeAsync(volume);
                return await EmbedHelper.MakeVolume(_client, user, player, volume, false);
            }
            else if (volume <= 100)
            {
                await player.UpdateVolumeAsync(volume);
                return await EmbedHelper.MakeVolume(_client, user, player, volume, false);
            }
            else
            {
                return await EmbedHelper.MakeVolume(_client, user, player, volume, true);
            }
        }

        public async Task<Embed> SetBassBoostAsync(IGuild guild, SocketUser commandUser)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player == null)
            {
                return await EmbedHelper.MakeFilter(_client, commandUser, null, "Bass Boost", true);
            }

            await player.EqualizerAsync(bassboost ? Filter.BassBoost(true) : Filter.BassBoost(false));
            bassboost = !bassboost;
            return await EmbedHelper.MakeFilter(_client, commandUser, player, "Bass Boost", false, bassboost);
        }

        public async Task<Embed> SetNightCoreAsync(IGuild guild, SocketUser commandUser)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player == null)
            {
                return await EmbedHelper.MakeFilter(_client, commandUser, null, "NightCore", true);
            }
            await player.ApplyFilterAsync(nightcore ? Filter.NightCore(true) : Filter.NightCore(false));
            nightcore = !nightcore;
            return await EmbedHelper.MakeFilter(_client, commandUser, player, "NightCore", false, nightcore);
        }
        
        public async Task<Embed> SetEightDAsync(IGuild guild, SocketUser commandUser)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player == null)
            {
                return await EmbedHelper.MakeFilter(_client, commandUser, null, "8D", true);
            }
            await player.ApplyFilterAsync(eightD ? Filter.EightD(true) : Filter.EightD(false));
            eightD = !eightD;
            return await EmbedHelper.MakeFilter(_client, commandUser, player, "8D", false, eightD);
        }

        public async Task<Embed> SetVaporWaveAsync(IGuild guild, SocketUser commandUser)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player == null)
            {
                return await EmbedHelper.MakeFilter(_client, commandUser, null, "VaporWave", true);
            }
            await player.ApplyFilterAsync(vaporwave ? Filter.VaporWave(true) : Filter.VaporWave(false));
            vaporwave = !vaporwave;
            return await EmbedHelper.MakeFilter(_client, commandUser, player, "VaporWave", false, vaporwave);
        }
        
        public async Task<Embed> SetKaraokeAsync(IGuild guild, SocketUser commandUser)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player == null)
            {
                return await EmbedHelper.MakeFilter(_client, commandUser, null, "Karaoke", true);
            }
            await player.ApplyFilterAsync(karaoke ? Filter.Karaoke(true) : Filter.Karaoke(false));
            karaoke = !karaoke;
            return await EmbedHelper.MakeFilter(_client, commandUser, player, "Karaoke", false, karaoke);
        }
        
        public async Task<Embed> SetLoopAsync(IGuild guild, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player is not {PlayerState: PlayerState.Playing})
            {
                return await EmbedHelper.MakeLoop(_client, user, null, true);
            }

            loop = !loop;
            return await EmbedHelper.MakeLoop(_client, user, player, loop);
        }

        public async Task<Embed> ClearFiltersAsync(IGuild guild, SocketUser commandUser)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player is not {PlayerState: PlayerState.Playing})
            {
                return await EmbedHelper.MakeFilter(_client, commandUser, null, "Clear", true);
            }
            IFilter[] filters = 
            { 
                Filter.NightCore(false), 
                Filter.EightD(false), 
                Filter.VaporWave(false), 
                Filter.Karaoke(false) 
            };
            await player.ApplyFiltersAsync(filters, 100, Filter.BassBoost(false));
            return await EmbedHelper.MakeFilter(_client, commandUser, player, "Clear", false, false);
        }
/*
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
        }*/

        public async Task<Embed> FastForward(TimeSpan time, IGuild guild, SocketUser user)
        {
            var player = _lavaNode.GetPlayer(guild);
            if (player == null)
            {
                return await EmbedHelper.MakeFastForward(_client, user, null, time, true);
            }
            await player.SeekAsync(time);
            return await EmbedHelper.MakeFastForward(_client, user, player, time, false);
        }

        private async Task OnReadyAsync()
        {
            await _lavaNode.ConnectAsync();
        }

        private static bool ShouldPlayNext(TrackEndReason trackEndReason)
        {
            return trackEndReason is TrackEndReason.Finished or TrackEndReason.LoadFailed;
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
                if (loop)
                    await player.PlayAsync(args.Track);
                //await player.TextChannel.SendMessageAsync("Queue completed! Please add more tracks to rock n' roll!");
                return;
            }

            if (queueable is not { } track)
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
                    Text = $"KBot | {DateTime.UtcNow}",
                    IconUrl = _client.CurrentUser.GetAvatarUrl()
                }
            };
            await player.TextChannel.SendMessageAsync(string.Empty, false, eb.Build());

        }
    }
}
