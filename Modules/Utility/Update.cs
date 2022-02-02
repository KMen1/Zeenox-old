using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using System.IO.Compression;

namespace KBot.Modules.Utility;

public class Update : InteractionModuleBase<SocketInteractionContext>
{
    private const string VersionUrl = "https://pastebin.com/raw/1gh1hT32";
    private const string UpdateUrl = "https://pastebin.com/raw/ru9hWYcj";
    [RequireOwner]
    [SlashCommand("update", "Frissíti a botot")]
    public async Task ResetAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        using var client = new HttpClient();
        var newVersion = await client.GetStringAsync(VersionUrl).ConfigureAwait(false);
        var currentVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
        if (newVersion != currentVersion)
        {
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = Context.Client.CurrentUser.Username,
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
                },
                Title = "Frissítés elérhető",
                Description = $"- Jelenlegi verzió: `{currentVersion}` \n- Új verzió: `{newVersion}`\n Biztosan frissíted a botot?"
            }.Build();
            var comp = new ComponentBuilder()
                .WithButton("Igen", "update-yes", ButtonStyle.Success, new Emoji("✅"))
                .WithButton("Nem", "update-no", ButtonStyle.Danger, new Emoji("❌"))
                .Build();
            await FollowupAsync(embed: eb, components: comp).ConfigureAwait(false);
        }
        else
        {
            await FollowupAsync("Nem érhető el új verzió.").ConfigureAwait(false);
        }
    }
    [ComponentInteraction("update-yes")]
    public async Task UpdateYesAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var bmsg = await Context.Interaction.GetOriginalResponseAsync().ConfigureAwait(false);
        await bmsg.DeleteAsync().ConfigureAwait(false);
        var msg = await bmsg.Channel.SendMessageAsync("Frissítés letöltése folyamatban...").ConfigureAwait(false);
        using var client = new HttpClient();
        var newVersion = await client.GetStringAsync(VersionUrl).ConfigureAwait(false);
        var uri = new Uri(await client.GetStringAsync(UpdateUrl).ConfigureAwait(false));
        Directory.CreateDirectory($"C:\\KBot\\{newVersion}");
        var response = await client.GetAsync(uri).ConfigureAwait(false);
        using (var fs = new FileStream($"C:\\KBot\\{newVersion}\\update.zip", FileMode.CreateNew))
        {
            await response.Content.CopyToAsync(fs).ConfigureAwait(false);
        }
        
        await msg.ModifyAsync(x => x.Content = "Frissítés letöltve...").ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false);
        await msg.ModifyAsync(x => x.Content = "Frissítés kitömörítése...").ConfigureAwait(false);
        ZipFile.ExtractToDirectory($"C:\\KBot\\{newVersion}\\update.zip", $"C:\\KBot\\{newVersion}");
        await msg.ModifyAsync(x => x.Content = "Frissítés kitömörítve...").ConfigureAwait(false);
        File.Delete($"C:\\KBot\\{newVersion}\\update.zip");
        await msg.ModifyAsync(x => x.Content = "Indítás...").ConfigureAwait(false);
        var pInfo = new ProcessStartInfo
        {
            UseShellExecute = true,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Normal,
            FileName = $"C:\\KBot\\{newVersion}\\KBot.exe"
        };
        await msg.ModifyAsync(x => x.Content = "KBot sikeresen frissítve!").ConfigureAwait(false);
        Process.Start(pInfo);
        Environment.Exit(0);
    }
    [ComponentInteraction("update-no")]
    public async Task UpdateNoAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await Context.Interaction.DeleteOriginalResponseAsync().ConfigureAwait(false);
    }
}