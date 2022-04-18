using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Interactions;

namespace KBot.Modules.Owner;

public class OwnerCommands : SlashModuleBase
{
    [RequireOwner]
    [SlashCommand("restart", "Restarts the bot")]
    public async Task ResetAsync()
    {
        var version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
        var pInfo = new ProcessStartInfo
        {
            UseShellExecute = true,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Normal,
            FileName = $"C:\\KBot\\{version}\\KBot.exe"
        };
        await RespondAsync("A bot újraindult.").ConfigureAwait(false);
        Process.Start(pInfo);
        Environment.Exit(0);
    }
}