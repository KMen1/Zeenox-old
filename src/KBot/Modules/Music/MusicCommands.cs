using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Extensions;

namespace KBot.Modules.Music;

[Group("music", "Music")]
public class MusicCommands : SlashModuleBase
{
    public AudioService AudioService { get; set; }

    [SlashCommand("move", "Moves the bot to the channel you are in")]
    public async Task MovePlayerAsync()
    {
        var channel = ((IVoiceState) Context.User).VoiceChannel;
        if (channel is null)
        {
            await RespondAsync("You are not in a voice channel", ephemeral: true);
            return;
        }

        await RespondAsync(embed: await AudioService.MoveAsync(Context.Guild, channel).ConfigureAwait(false), ephemeral: true)
            .ConfigureAwait(false);
    }

    [SlashCommand("leave", "Leaves the voice channel the bot is in")]
    public async Task DisconnectPlayerAsync()
    {
        await RespondAsync(embed: await AudioService.DisconnectAsync(Context.Guild, Context.User).ConfigureAwait(false), ephemeral: true)
            .ConfigureAwait(false);
    }

    [SlashCommand("play", "Plays a song")]
    public async Task PlayAsync(string query)
    {
        await DeferAsync().ConfigureAwait(false);
        if (((IVoiceState) Context.User).VoiceChannel is null)
        {
            await FollowupAsync(embed: new EmbedBuilder().ErrorEmbed("You are not in a voice channel!"))
                .ConfigureAwait(false);
            return;
        }

        await AudioService.PlayAsync(Context.Guild, Context.Interaction, query).ConfigureAwait(false);
    }

    [SlashCommand("search", "Searches for a song")]
    public async Task SearchAsync(string query)
    {
        await DeferAsync().ConfigureAwait(false);
        if (Uri.IsWellFormedUriString(query, UriKind.Absolute))
        {
            await AudioService.PlayAsync(Context.Guild, Context.Interaction, query).ConfigureAwait(false);
            return;
        }

        var search = await AudioService.SearchAsync(query).ConfigureAwait(false);
        if (search is null)
        {
            await FollowupAsync("No matches!").ConfigureAwait(false);
            return;
        }

        var tracks = search.Tracks.ToList(); //
        var desc = tracks.Take(10).Aggregate("",
            (current, track) =>
                current +
                $"{tracks.TakeWhile(n => n != track).Count() + 1}. [`{track.Title}`]({track.Source}) | [`{track.Duration}`]\n");

        var comp = new ComponentBuilder();
        for (var i = 0; i < tracks.Take(10).Count(); i++)
        {
            var emoji = i switch
            {
                0 => "1️⃣",
                1 => "2️⃣",
                2 => "3️⃣",
                3 => "4️⃣",
                4 => "5️⃣",
                5 => "6️⃣",
                6 => "7️⃣",
                7 => "8️⃣",
                8 => "9️⃣",
                9 => "🔟",
                _ => ""
            };
            comp.WithButton(" ", $"search:{tracks[i].TrackIdentifier}", emote: new Emoji(emoji));
        }

        var eb = new EmbedBuilder()
            .WithTitle("Search Results")
            .WithColor(Color.Blue)
            .WithDescription(desc)
            .Build();
        await FollowupAsync(embed: eb, components: comp.Build()).ConfigureAwait(false);
    }

    [ComponentInteraction("search:*", true)]
    public async Task PlaySearchAsync(string identifier)
    {
        var channel = ((IVoiceState) Context.User).VoiceChannel;
        if (channel is null)
        {
            await RespondAsync("You are not in a voice channel", ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        await AudioService.PlayFromSearchAsync(Context.Guild, (SocketMessageComponent) Context.Interaction, identifier)
            .ConfigureAwait(false);
    }

    [SlashCommand("volume", "Sets the volume")]
    public async Task ChangeVolumeAsync(
        [Summary("volume")] [MinValue(1)] [MaxValue(100)]
        ushort volume)
    {
        await RespondAsync(embed: await AudioService.SetVolumeAsync(Context.Guild, volume).ConfigureAwait(false),
            ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("queue", "Shows the current queue")]
    public Task SendQueueAsync()
    {
        return RespondAsync(embed: AudioService.GetQueue(Context.Guild), ephemeral: true);
    }

    [SlashCommand("clearqueue", "Clears the current queue")]
    public async Task ClearQueueAsync()
    {
        await RespondAsync(embed: await AudioService.ClearQueueAsync(Context.Guild).ConfigureAwait(false), ephemeral: true)
            .ConfigureAwait(false);
    }

    [SlashCommand("autoplay", "Toggles autoplay")]
    public async Task ToggleAutoplayAsync()
    {
        await RespondAsync(embed: await AudioService.ToggleAutoplayAsync(Context.Guild).ConfigureAwait(false),
            ephemeral: true).ConfigureAwait(false);
    }
}