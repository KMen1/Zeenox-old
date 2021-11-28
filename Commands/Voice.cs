using Discord;
using Discord.Net;
using Discord.WebSocket;
using KBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KBot.Commands
{
    public class VoiceCommands
    {
        private readonly AudioService _audioService;
        private readonly DiscordSocketClient _client;
        public VoiceCommands(IServiceProvider services)
        {
            _audioService = services.GetRequiredService<AudioService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
        }

        public async Task MakeVoiceCommands()
        {
            var newCommands = new[] {
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
            };

            //var guild = _client.GetGuild(863751874922676234);
            var globalCommands = await _client.GetGlobalApplicationCommandsAsync();

            List<string> existingCommandsName = new List<string>();
            foreach (var command in globalCommands)
            {
                existingCommandsName.Add(command.Name);
            }

            foreach (SlashCommandBuilder newCommand in newCommands)
            {
                try
                {
                    if (!existingCommandsName.Contains(newCommand.Name))
                        await _client.CreateGlobalApplicationCommandAsync(newCommand.Build());
                    //await _client.(command.Build());
                }
                catch (HttpException exception)
                {
                    var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                    Console.WriteLine(json);
                }
            };
        }

        public async Task HandleJoinCommand(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: await _audioService.JoinAsync((slashCommand.Channel as ITextChannel).Guild, (slashCommand.User as IVoiceState).VoiceChannel, slashCommand.Channel as ITextChannel, slashCommand.User));
        }
        public async Task HandleLeaveCommand(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: await _audioService.LeaveAsync((slashCommand.User as IVoiceState).VoiceChannel, slashCommand.User));
        }
        public async Task HandlePlayCommand(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: await _audioService.PlayAsync((string)slashCommand.Data.Options.First().Value, (slashCommand.Channel as ITextChannel).Guild, (slashCommand.User as IVoiceState).VoiceChannel, slashCommand.Channel as ITextChannel, slashCommand.User));
        }

        public async Task HandlePauseCommand(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: await _audioService.PauseOrResumeAsync((slashCommand.Channel as ITextChannel).Guild, slashCommand.User));
        }

        public async Task HandleResumeCommand(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: await _audioService.PauseOrResumeAsync((slashCommand.Channel as ITextChannel).Guild, slashCommand.User));
        }

        public async Task HandleSkipCommand(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: await _audioService.SkipAsync((slashCommand.Channel as ITextChannel).Guild, slashCommand.User));
        }

        public async Task HandleStopCommand(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: await _audioService.StopAsync((slashCommand.Channel as ITextChannel).Guild, slashCommand.User));
        }
        public async Task HandleMoveCommand(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: await _audioService.MoveAsync((slashCommand.Channel as ITextChannel).Guild, (slashCommand.User as IVoiceState).VoiceChannel, slashCommand.User));
        }
        public async Task HandleVolumeCommand(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(embed: await _audioService.SetVolumeAsync((ushort)slashCommand.Data.Options.First().Value, (slashCommand.Channel as ITextChannel).Guild, slashCommand.User));
        }

    }

    /*public class Voice : ModuleBase<SocketCommandContext>
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
    }*/
}
