using Discord;
using Discord.Commands;
using KBot.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KBot.Commands
{
    public class Voice : ModuleBase<SocketCommandContext>
    {
        private readonly AudioService _service;
        public Voice(IServiceProvider services)
        {
            _service = services.GetRequiredService<AudioService>();
        }

        [Command("join"), Alias("j"), Summary("Csatlakozik ahhoz a hangcsatornához, amelyben tartózkodsz")]
        public async Task JoinChannel()
        {
            await Context.Message.DeleteAsync();
            await ReplyAsync(embed: await _service.JoinAsync(Context.Guild, (Context.User as IVoiceState).VoiceChannel, Context.Channel as ITextChannel, Context.User));
        }

        [Command("leave"), Alias("l"), Summary("Elhagyja azt a hangcsatornát, amelyben a bot jelenleg tartózkodik")]
        public async Task LeaveChannel()
        {
            await Context.Message.DeleteAsync();
            await ReplyAsync(embed: await _service.LeaveAsync((Context.User as IVoiceState).VoiceChannel, Context.User));
        }

        [Command("play"), Alias("p"), Summary("Lejátssza a kívánt zenét")]
        public async Task PlaySong([Remainder] string query)
        {
            await Context.Message.DeleteAsync();
            await ReplyAsync(embed: await _service.PlayAsync(query, Context.Guild, (Context.User as IVoiceState).VoiceChannel, Context.Channel as ITextChannel, Context.User));
        }

        [Command("stop"), Alias("sp"), Summary("Zenelejátszás megállítása")]
        public async Task StopSong()
        {
            await Context.Message.DeleteAsync();
            await ReplyAsync(embed: await _service.StopAsync(Context.Guild, Context.User));
        }

        [Command("move"), Alias("m"), Summary("Átlép abba a hangcsatornába, amelyben tartózkodsz")]
        public async Task MoveAsync()
        {
            await Context.Message.DeleteAsync();
            await ReplyAsync(embed: await _service.MoveAsync(Context.Guild, (Context.User as IVoiceState).VoiceChannel, Context.User));
        }

        [Command("skip"), Alias("s"), Summary("Lejátsza a következő zenét a sorban")]
        public async Task SkipAsync()
        {
            await Context.Message.DeleteAsync();
            await ReplyAsync(embed: await _service.SkipAsync(Context.Guild, Context.User));
        }

        [Command("pause"), Alias("p"), Summary("Zenelejátszás szüneteltetése")]
        public async Task PauseAsync()
        {
            await Context.Message.DeleteAsync();
            await ReplyAsync(embed: await _service.PauseOrResumeAsync(Context.Guild, Context.User));
        }

        [Command("resume"), Alias("r"), Summary("Zenelejátszás folytatása")]
        public async Task ResumeAsync()
        {
            await Context.Message.DeleteAsync();
            await ReplyAsync(embed: await _service.PauseOrResumeAsync(Context.Guild, Context.User));
        }

        [Command("volume"), Alias("v", "vol"), Summary("Hangerő beállítása (óvatosan)")]
        public async Task SetVolumeAsync(ushort volume)
        {
            await Context.Message.DeleteAsync();
            await ReplyAsync(embed: await _service.SetVolume(volume, Context.Guild, (long)Context.User.Id, Context.User));
        }
        [Command("bass"), Alias("bb"), Summary("Basszus erősítése")]
        public async Task BassBoostAsync()
        {
            await Context.Message.DeleteAsync();
            await ReplyAsync(embed: await _service.SetBassBoost(Context.Guild, Context.User));
        }

        [Command("forward"), Alias("fw"), Summary("Előretekeri a jelenleg játszott zenét a kívánt időre")]
        public async Task FastForward(string newTime)
        {
            await Context.Message.DeleteAsync();
            await ReplyAsync(embed: await _service.FastForward(TimeSpan.Parse("00:" + newTime), Context.Guild, Context.User));
        }
        [Command("muteall"), Alias("mute"), Summary("Lenémít/unmutol mindenkit aki a bottal együtt van hangcsatornában")]
        [RequireOwner]
        public async Task MuteAllMembers(bool isMuted)
        {
            var v = Context.Guild.VoiceChannels.First(x =>
                x.Id.Equals((Context.User as IVoiceState).VoiceChannel.Id)).Users;

            foreach (var user in v)
            {
                await user.ModifyAsync(x => x.Deaf = isMuted);
            }
            await ReplyAsync("Kész!");
        }
    }
}
