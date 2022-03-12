using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Lavalink4NET.Player;

namespace KBot.Modules.Music.Helpers;

public static class Embeds
{
    private const string SuccessIcon = "https://i.ibb.co/HdqsDXh/tick.png";
    private const string ErrorIcon = "https://i.ibb.co/SrZZggy/x.png";
    private const string PlayingGif = "https://bestanimations.com/media/discs/895872755cd-animated-gif-9.gif";

    public static Embed LeaveEmbed(IVoiceChannel vChannel)
    {
        return new EmbedBuilder()
            .WithAuthor("SIKERES ELHAGYÁS", SuccessIcon)
            .WithDescription($"A következő csatornából: {vChannel.Mention}")
            .WithColor(Color.Green)
            .Build();
    }

    public static Embed MoveEmbed(IVoiceChannel vChannel)
    {
        return new EmbedBuilder()
            .WithAuthor("SIKERES MOZGATÁS", SuccessIcon)
            .WithDescription($"A következő csatornába: {vChannel.Mention}")
            .WithColor(Color.Green)
            .Build();
    }

    public static Embed NowPlayingEmbed(SocketUser user, MusicPlayer player)
    {
        var eb = new EmbedBuilder()
            .WithAuthor("MOST JÁTSZOTT", PlayingGif)
            .WithTitle(player.CurrentTrack.Title)
            .WithUrl(player.CurrentTrack.Source)
            .WithImageUrl($"https://img.youtube.com/vi/{player.CurrentTrack.TrackIdentifier}/maxresdefault.jpg")
            .WithColor(Color.Green)
            .AddField("👨 Hozzáadta", user.Mention, true)
            .AddField("🔼 Feltöltötte", $"`{player.CurrentTrack.Author}`", true)
            .AddField("🎙️ Csatorna", player.VoiceChannel.Mention, true)
            .AddField("🕐 Hosszúság", $"`{player.CurrentTrack.Duration.ToString("c")}`", true)
            .AddField("🔁 Ismétlés", player.LoopEnabled ? "`Igen`" : "`Nem`", true)
            .AddField("🔊 Hangerő", $"`{Math.Round(player.Volume * 100).ToString()}%`", true)
            .AddField("📝 Szűrő", player.FilterEnabled is not null ? $"`{player.FilterEnabled}`" : "`Nincs`", true)
            .AddField("🎶 Várólistán", $"`{player.Queue.Count.ToString()}`", true)
            .Build();
        return eb;
    }

    public static Embed VolumeEmbed(MusicPlayer player)
    {
        return new EmbedBuilder()
            .WithAuthor($"HANGERŐ {player.Volume.ToString()}%-RA ÁLLÍTVA", SuccessIcon)
            .WithDescription($"Ebben a csatornában: {player.VoiceChannel.Mention}")
            .WithColor(Color.Green)
            .Build();
    }

    public static Embed QueueEmbed(MusicPlayer player, bool cleared = false)
    {
        var eb = new EmbedBuilder()
            .WithAuthor(cleared ? "LEJÁTSZÁSI LISTA TÖRÖLVE" : "LEJÁTSZÁSI LISTA LEKÉRVE", SuccessIcon)
            .WithDescription($"Ebben a csatornában: {player.VoiceChannel.Mention}")
            .WithColor(Color.Green);
        if (cleared)
        {
            return eb.Build();
        }
        if (player.Queue.Count == 0)
        {
            eb.WithDescription("`Nincs zene a lejátszási listában`");
        }
        else
        {
            var desc = new StringBuilder();
            foreach (var track in player.Queue)
            {
                desc.AppendLine(//
                    $":{(player.Queue.TakeWhile(n => n != track).Count() + 1).ToWords()}: [`{track.Title}`]({track.Source}) | Hozzáadta: {((MusicPlayer.TrackContext)track.Context)!.AddedBy.Mention}");
            }

            eb.WithDescription(desc.ToString());
        }
        return eb.Build();
    }

    public static Embed AddedToQueueEmbed(List<LavalinkTrack> tracks)
    {
        var desc = tracks.Take(10).Aggregate("", (current, track) => current + $"{tracks.TakeWhile(n => n != track).Count() + 1}. [`{track.Title}`]({track.Source})\n");
        if (tracks.Count > 10)
        {
            desc += $"és még {(tracks.Count - 10).ToString()} zene\n";
        }
        return new EmbedBuilder()
            .WithAuthor($"{tracks.Count} SZÁM HOZZÁADVA A VÁRÓLISTÁHOZ", SuccessIcon)
            .WithColor(Color.Orange)
            .WithDescription(desc)
            .Build();
    }

    public static Embed ErrorEmbed(string exception)
    {
        return new EmbedBuilder()
            .WithAuthor("HIBA", ErrorIcon)
            .WithTitle("Hiba történt a parancs végrehajtása során")
            .WithDescription("Kérlek próbáld meg újra! \n" +
                             "Ha a hiba továbbra is fennáll, kérlek jelezd a <@132797923049209856>-nek!")
            .WithColor(Color.Red)
            .AddField("Hibaüzenet", $"```{exception}```")
            .Build();
    }
}