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
        await RespondAsync("A bot újra használható lesz 1 percen belül.");
        var info = new ProcessStartInfo
        {
            Arguments = System.Reflection.Assembly.GetExecutingAssembly().Location,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            FileName = "dotnet.exe"
        };
        //Process.Start(info);
        //Environment.Exit(0);
    }
    
}