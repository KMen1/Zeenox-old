using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.Owner;

public class OwnerCommands : SlashModuleBase
{
    private const string VersionUrl = "https://pastebin.com/raw/1gh1hT32";
    private const string UpdateUrl = "https://pastebin.com/raw/ru9hWYcj";

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
        await RespondAsync("Restarted", ephemeral: true).ConfigureAwait(false);
        Process.Start(pInfo);
        Environment.Exit(0);
    }

    [RequireOwner]
    [SlashCommand("update", "Frissíti a botot")]
    public async Task UpdateAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        using var client = new HttpClient();
        var newVersion = await client.GetStringAsync(VersionUrl).ConfigureAwait(false);
        var currentVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
        if (!newVersion.Equals(currentVersion, StringComparison.OrdinalIgnoreCase))
        {
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = Context.Client.CurrentUser.Username,
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
                },
                Title = "Update available",
                Description =
                    $"- Current version: `{currentVersion}` \n- New Version: `{newVersion}`\nAre you sure you want to update?"
            }.Build();
            var comp = new ComponentBuilder()
                .WithButton("Confirm", "update-yes", ButtonStyle.Success, new Emoji("✅"))
                .WithButton("Cancel", "update-no", ButtonStyle.Danger, new Emoji("❌"))
                .Build();
            await FollowupAsync(embed: eb, components: comp).ConfigureAwait(false);
            return;
        }

        await FollowupAsync($"The current version is the latest ({currentVersion})").ConfigureAwait(false);
    }

    [ComponentInteraction("update-yes")]
    public async Task UpdateYesAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var bmsg = await Context.Interaction.GetOriginalResponseAsync().ConfigureAwait(false);
        await bmsg.DeleteAsync().ConfigureAwait(false);
        var msg = await bmsg.Channel.SendMessageAsync("Downloading update...").ConfigureAwait(false);
        using var client = new HttpClient();
        var newVersion = await client.GetStringAsync(VersionUrl).ConfigureAwait(false);
        var uri = new Uri(await client.GetStringAsync(UpdateUrl).ConfigureAwait(false));
        Directory.CreateDirectory($"C:\\KBot\\{newVersion}");
        var response = await client.GetAsync(uri).ConfigureAwait(false);
        using (var fs = new FileStream($"C:\\KBot\\{newVersion}\\update.zip", FileMode.CreateNew))
        {
            await response.Content.CopyToAsync(fs).ConfigureAwait(false);
        }

        await msg.ModifyAsync(x => x.Content = "Downloaded update...").ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false);
        await msg.ModifyAsync(x => x.Content = "Unpacking update...").ConfigureAwait(false);
        ZipFile.ExtractToDirectory($"C:\\KBot\\{newVersion}\\update.zip", $"C:\\KBot\\{newVersion}");
        await msg.ModifyAsync(x => x.Content = "Unpacked update...").ConfigureAwait(false);
        File.Delete($"C:\\KBot\\{newVersion}\\update.zip");
        await msg.ModifyAsync(x => x.Content = "Starting bot...").ConfigureAwait(false);
        var pInfo = new ProcessStartInfo
        {
            UseShellExecute = true,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Normal,
            FileName = $"C:\\KBot\\{newVersion}\\KBot.exe"
        };
        await msg.ModifyAsync(x => x.Content = "Bot updated!").ConfigureAwait(false);
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