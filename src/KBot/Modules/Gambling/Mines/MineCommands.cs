﻿using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Models;

namespace KBot.Modules.Gambling.Mines;

[Group("mine", "Roobet Mine")]
public class MineCommands : SlashModuleBase
{
    public MinesService MinesService { get; set; }

    [SlashCommand("start", "Starts a new game of mine")]
    public async Task StartMinesAsync([MinValue(100)] [MaxValue(1000000)] int bet,
        [MinValue(5)] [MaxValue(24)] int mines)
    {
        await DeferAsync().ConfigureAwait(false);
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)Context.User).ConfigureAwait(false);
        if (dbUser.Balance < bet)
        {
            await FollowupAsync("Insufficient balance.").ConfigureAwait(false);
            return;
        }

        var msg = await FollowupAsync("Starting...").ConfigureAwait(false);
        var game = MinesService.CreateGame(Context.User, msg, bet, mines);
        await game.StartAsync().ConfigureAwait(false);
    }

    [SlashCommand("stop", "Stops the specified game")]
    public async Task StopMinesAsync(string id)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var game = MinesService.GetGame(id);
        if (game is null)
        {
            await FollowupAsync("No game found for that id.").ConfigureAwait(false);
            return;
        }

        if (game.User.Id != Context.User.Id)
            await FollowupAsync("You can't stop another players game!").ConfigureAwait(false);
        if (!game.CanStop)
        {
            await FollowupAsync("You need to click at least one filed to be able to stop the game.")
                .ConfigureAwait(false);
            return;
        }

        await game.StopAsync(false).ConfigureAwait(false);
        await FollowupAsync("Stopped!").ConfigureAwait(false);
    }
}