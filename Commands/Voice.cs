using Discord;
using Discord.WebSocket;
using KBot.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KBot.Commands
{
    public class VoiceCommands
    {
        private readonly Audio _audioService;

        public VoiceCommands(IServiceProvider services)
        {
            _audioService = services.GetRequiredService<Audio>();
        }

        public static Task<SlashCommandBuilder[]> MakeVoiceCommands()
        {
            return Task.Run(() =>
            {
                var newCommands = new[] 
                {
                    new SlashCommandBuilder()
                        .WithName("join")
                        .WithDescription("Csatlakozik ahhoz a hangcsatornához, amelyben tartózkodsz"),
                    new SlashCommandBuilder()
                        .WithName("leave")
                        .WithDescription("Elhagyja azt a hangcsatornát, amelyben a bot jelenleg tartózkodik"),
                    new SlashCommandBuilder()
                        .WithName("play")
                        .WithDescription("Lejátssza a kívánt zenét")
                        .AddOption("query", ApplicationCommandOptionType.String, "Zene linkje vagy címe", isRequired: true),
                    new SlashCommandBuilder()
                        .WithName("stop")
                        .WithDescription("Zenelejátszás megállítása"),
                    new SlashCommandBuilder()
                        .WithName("move")
                        .WithDescription("Átlép abba a hangcsatornába, amelyben tartózkodsz"),
                    new SlashCommandBuilder()
                        .WithName("skip")
                        .WithDescription("Lejátsza a következő zenét a sorban"),
                    new SlashCommandBuilder()
                        .WithName("pause")
                        .WithDescription("Zenelejátszás szüneteltetése"),
                    new SlashCommandBuilder()
                        .WithName("resume")
                        .WithDescription("Zenelejátszás folytatása"),
                    new SlashCommandBuilder()
                        .WithName("volume")
                        .WithDescription("Hangerő beállítása")
                        .AddOption("volume", ApplicationCommandOptionType.Integer, "Hangerő számban megadva (1-100)", isRequired: true, minValue: 1, maxValue: 100),
                    /*new SlashCommandBuilder()
                        .WithName("filter")
                        .WithDescription("Filterek állítása")
                        .AddOption(
                        new SlashCommandOptionBuilder()
                            .WithName("filter")
                            .WithDescription("")
                            .WithRequired(true)
                            .AddChoice("ChannelMix", "ChannelMix")
                            .AddChoice("Distortion", "Distortion")
                            .AddChoice("Karoke", "Karoke")
                            .AddChoice("LowPass", "LowPass")
                            .AddChoice("Rotation", "Rotation")
                            .AddChoice("Timescale", "Timescale")
                            .AddChoice("Tremolo", "Tremolo")
                            .AddChoice("Vibrato", "Vibrato")
                            .WithType(ApplicationCommandOptionType.String)
                        )*/
                };
                return newCommands;
            });
        }

        public async Task Join(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: 
                await _audioService.JoinAsync(
                    ((ITextChannel) slashCommand.Channel).Guild, 
                    ((IVoiceState) slashCommand.User).VoiceChannel, 
                    (ITextChannel) slashCommand.Channel, 
                    slashCommand.User));
        }
        public async Task Leave(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: 
                await _audioService.LeaveAsync(
                    ((ITextChannel) slashCommand.Channel).Guild,
                    ((IVoiceState) slashCommand.User).VoiceChannel, 
                    slashCommand.User));
        }
        public async Task Play(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: 
                await _audioService.PlayAsync(
                    (string)slashCommand.Data.Options.First().Value, 
                    ((ITextChannel) slashCommand.Channel).Guild, 
                    ((IVoiceState) slashCommand.User).VoiceChannel, 
                    slashCommand.Channel as ITextChannel, 
                    slashCommand.User));
        }

        public async Task Pause(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: 
                await _audioService.PauseOrResumeAsync(
                    ((ITextChannel) slashCommand.Channel).Guild, 
                    slashCommand.User));
        }

        public async Task Resume(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: 
                await _audioService.PauseOrResumeAsync(
                    ((ITextChannel) slashCommand.Channel).Guild, 
                    slashCommand.User));
        }

        public async Task Skip(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: 
                await _audioService.SkipAsync(
                    ((ITextChannel) slashCommand.Channel).Guild, 
                    slashCommand.User));
        }

        public async Task Stop(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: 
                await _audioService.StopAsync(
                    ((ITextChannel) slashCommand.Channel).Guild, 
                    slashCommand.User));
        }
        public async Task Move(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: 
                await _audioService.MoveAsync(
                    ((ITextChannel) slashCommand.Channel).Guild, 
                    ((IVoiceState) slashCommand.User).VoiceChannel, 
                    slashCommand.User));
        }
        public async Task Volume(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: await _audioService.SetVolumeAsync((ushort)slashCommand.Data.Options.First().Value, ((ITextChannel) slashCommand.Channel).Guild, slashCommand.User));
        }

        /*public async Task Filter(string filter, SocketSlashCommand slashCommand)
        {
            switch (filter)
            {
                case "":
                    break;
            }
        }*/
    }

    /*public class Voice : ModuleBase<SocketCommandContext>
    {
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
    }*/
}
