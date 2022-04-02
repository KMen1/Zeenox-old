﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.Utility;

public class Reset : KBotModuleBase
{
    [RequireOwner]
    [SlashCommand("reset", "Újraindítja a botot")]
    public async Task ResetAsync()
    {
        var psi = new ProcessStartInfo("cmd.exe");
        var path = "dotnet " + Environment.CurrentDirectory + @"\KBot.dll";
        psi.UseShellExecute = true;
        psi.Arguments = $"/k {path}";
        Process.Start(psi);
        await RespondAsync("A bot újraindult.").ConfigureAwait(false);
        Environment.Exit(0);
    }
}