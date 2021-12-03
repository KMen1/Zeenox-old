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
                        .WithName("queue")
                        .WithDescription("A sorban lévő zenék listája"),
                    new SlashCommandBuilder()
                        .WithName("clearqueue")
                        .WithDescription("A sorban lévő zenék törlése"),*/
                    new SlashCommandBuilder()
                        .WithName("bassboost")
                        .WithDescription("Basszus erősítés bekapcsolása / kikapcsolása"),
                    new SlashCommandBuilder()
                        .WithName("nightcore")
                        .WithDescription("Nightcore mód bekapcsolása / kikapcsolása"),
                    new SlashCommandBuilder()
                        .WithName("8d")
                        .WithDescription("8D mód bekapcsolása / kikapcsolása"),
                    new SlashCommandBuilder()
                        .WithName("vaporwave")
                        .WithDescription("Vaporwave mód bekapcsolása / kikapcsolása"),
                    new SlashCommandBuilder()
                        .WithName("speed")
                        .WithDescription("Zene sebességének beállítása"),
                    new SlashCommandBuilder()
                        .WithName("pitch")
                        .WithDescription("Zene hangmagasságának beállítása"),
                    new SlashCommandBuilder()
                        .WithName("loop")
                        .WithDescription("Zene ismétlésének bekapcsolása / kikapcsolása"),
                    new SlashCommandBuilder()
                        .WithName("karaoke")
                        .WithDescription("Karaoke mód bekapcsolása / kikapcsolása"),
                    new SlashCommandBuilder()
                        .WithName("clearfilter")
                        .WithDescription("Minden aktív szűrőt deaktivál"),
                    /*new SlashCommandBuilder()
                        .WithName("247")
                        .WithDescription("24/7 mód bekapcsolása"),*/
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
                    (ITextChannel) slashCommand.Channel, 
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
            await slashCommand.RespondAsync(embed: 
                await _audioService.SetVolumeAsync(
                    (ushort)slashCommand.Data.Options.First().Value, 
                    ((ITextChannel) slashCommand.Channel).Guild, 
                    slashCommand.User));
        }

        public async Task Loop(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: 
                await _audioService.SetLoopAsync(
                    ((ITextChannel) slashCommand.Channel).Guild, 
                    slashCommand.User));
        }
        
        /*public async Task TwentyFourSeven(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: 
                await _audioService.SetTwentyFourSevenAsync(
                    ((ITextChannel) slashCommand.Channel).Guild, 
                    slashCommand.User));
        }*/
        
        public async Task BassBoost(SocketSlashCommand command)
        {
            await command.RespondAsync(embed: 
                await _audioService.SetBassBoostAsync(
                    ((ITextChannel)command.Channel).Guild, 
                    command.User));
        }

        public async Task NightCore(SocketSlashCommand command)
        {
            await command.RespondAsync(embed: 
                await _audioService.SetNightCoreAsync(
                    ((ITextChannel)command.Channel).Guild, 
                    command.User));
        }

        public async Task EightD(SocketSlashCommand command)
        {
            await command.RespondAsync(
                embed: await _audioService.SetEightDAsync(
                    ((ITextChannel)command.Channel).Guild, 
                    command.User));
        }

        public async Task VaporWave(SocketSlashCommand command)
        {
            await command.RespondAsync(embed: 
                await _audioService.SetVaporWaveAsync(
                    ((ITextChannel)command.Channel).Guild, 
                    command.User));
        }

        /*public async Task Speed(SocketSlashCommand command)
        {
            await command.RespondAsync(embed: 
                await _audioService.SetSpeedAsync(
                    (float)command.Data.Options.First().Value, 
                    ((ITextChannel)command.Channel).Guild, 
                    command.User));
        }
        
        public async Task Pitch(SocketSlashCommand command)
        {
            await command.RespondAsync(embed: 
                await _audioService.SetPitchAsync(
                    (float)command.Data.Options.First().Value, 
                    ((ITextChannel)command.Channel).Guild, 
                    command.User));
        }*/
        public async Task Karaoke(SocketSlashCommand command)
        {
            await command.RespondAsync(embed: 
                await _audioService.SetKaraokeAsync(
                    ((ITextChannel)command.Channel).Guild, 
                    command.User));
        }

        public async Task ClearFilters(SocketSlashCommand command)
        {
            await command.RespondAsync(embed: 
                await _audioService.ClearFiltersAsync(
                    ((ITextChannel)command.Channel).Guild, 
                    command.User));
        }
    }
}
