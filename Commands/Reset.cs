using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord.Interactions;

namespace KBot.Commands;

public class Reset : InteractionModuleBase<InteractionContext>
{
    [SlashCommand("reset", "Újraindítja a botot")]
    public async Task ResetAsync()
    {
        await RespondAsync("A bot újraindult.");
        var psi = new ProcessStartInfo("cmd.exe");
        string path = "dotnet " + Environment.CurrentDirectory + @"\KBot.dll";
        psi.UseShellExecute = true;
        psi.Arguments = $"/k {path}";
        Process.Start(psi);
        Environment.Exit(0);
    }
    
}