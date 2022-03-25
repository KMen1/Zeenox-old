using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Discord;
using Discord.WebSocket;
using Humanizer;
using KBot.Enums;
using KBot.Models;
using KBot.Modules.Gambling.BlackJack;
using KBot.Modules.Gambling.Crash;
using KBot.Modules.Gambling.HighLow;
using KBot.Modules.Music;
using Lavalink4NET.Player;

namespace KBot;

public static class Extensions
{
    private const string SuccessIcon = "https://i.ibb.co/HdqsDXh/tick.png";
    private const string ErrorIcon = "https://i.ibb.co/SrZZggy/x.png";
    private const string PlayingGif = "https://bestanimations.com/media/discs/895872755cd-animated-gif-9.gif";

    public static string GetDescription(this Enum value)
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value);
        if (name != null)
        {
            var field = type.GetField(name);
            if (field != null)
            {
                var attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attr != null)
                {
                    return attr.Description;
                }
            }
        }
        return null;
    }
    
    public static DateTimeOffset GetNextWeekday(this DateTime date, DayOfWeek day)
    {
        var result = date.Date.AddDays(1);
        while( result.DayOfWeek != day )
            result = result.AddDays(1);
        return result;
    }
    
    public static object GetModuleConfigFromGuildConfig(this GuildConfig config, ModuleType module)
    {
        return module switch
        {
            ModuleType.Announcements => config.Announcements,
            ModuleType.TemporaryVoice => config.TemporaryChannels,
            ModuleType.Leveling => config.Leveling,
            ModuleType.MovieEvents => config.MovieEvents,
            ModuleType.TourEvents => config.TourEvents,
            ModuleType.Suggestions => config.Suggestions,
            _ => throw new ArgumentOutOfRangeException(nameof(module), module, null)
        };
    }
    
    public static Embed MovieEventEmbed(this EmbedBuilder builder, SocketGuildEvent guildEvent, EventState embedType)
    {
        builder.WithTitle(guildEvent.Name)
            .WithDescription(guildEvent.Description)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .AddField("👨 Létrehozta", guildEvent.Creator.Mention, true)
            .AddField("📅 Időpont", guildEvent.StartTime.ToString("yyyy. MM. dd. HH:mm"), true)
            .AddField("🎙 Csatorna", ((SocketVoiceChannel)guildEvent.Channel).Mention, true);
        switch (embedType)
        {
            case EventState.Scheduled:
            {
                builder.WithAuthor("ÚJ FILM ESEMÉNY ÜTEMEZVE!", SuccessIcon).WithColor(Color.Orange);
                break;
            }
            case EventState.Updated:
            {
                builder.WithAuthor("FILM ESEMÉNY FRISSÍTVE!", SuccessIcon).WithColor(Color.Orange);
                break;
            }
            case EventState.Started:
            {
                builder.WithAuthor("FILM ESEMÉNY KEZDŐDIK!", SuccessIcon).WithColor(Color.Green);
                break;
            }
            case EventState.Cancelled:
            {
                builder.WithAuthor("FILM ESEMÉNY TÖRÖLVE!", ErrorIcon).WithColor(Color.Red);
                break;
            }
        }
        return builder.Build();
    }

    public static Embed TourEventEmbed(this EmbedBuilder builder, SocketGuildEvent guildEvent, EventState tourEmbedType)
    {
        builder.WithTitle(guildEvent.Name)
            .WithDescription(guildEvent.Description)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .AddField("👨 Létrehozta", guildEvent.Creator.Mention, true)
            .AddField("📅 Időpont", guildEvent.StartTime.ToString("yyyy. MM. dd. HH:mm"), true)
            .AddField("⛺ Helyszín", guildEvent.Location, true);
        switch (tourEmbedType)
        {
            case EventState.Scheduled:
            {
                builder.WithAuthor("ÚJ TÚRA ESEMÉNY ÜTEMEZVE!", SuccessIcon).WithColor(Color.Orange);
                break;
            }
            case EventState.Updated:
            {
                builder.WithAuthor("TÚRA ESEMÉNY FRISSÍTVE!", SuccessIcon).WithColor(Color.Orange);
                break;
            }
            case EventState.Started:
            {
                builder.WithAuthor("TÚRA ESEMÉNY KEZDŐDIK!", SuccessIcon).WithColor(Color.Green);
                break;
            }
            case EventState.Cancelled:
            {
                builder.WithAuthor("TÚRA ESEMÉNY TÖRÖLVE!", ErrorIcon).WithColor(Color.Red);
                break;
            }
        }
        return builder.Build();
    }
    
    public static Embed BlackJackEmbed(this EmbedBuilder builder, BlackJackGame game, string desc = null, Color color = default)
    {
        return builder.WithTitle($"Blackjack - {game.Id}")
            .WithDescription($"Tét: **{game.Stake} kredit**\n{desc}")
            .WithColor(color == default ? Color.Gold : color)
            .WithImageUrl(game.GetTablePicUrl())
            .AddField("Játékos", $"Érték: `{game.PlayerScore.ToString()}`", true)
            .AddField("Osztó", game.Hidden ? "Érték: `?`" : $"Érték: `{game.DealerScore.ToString()}`", true)
            .Build();
    }
    
    public static Embed HighLowEmbed(this EmbedBuilder builder, HighLowGame game, string desc = null, Color color = default)
    {
        return builder.WithTitle($"Higher/Lower - {game.Id}")
            .WithDescription($"Tét: **{game.Stake} kredit**\n{desc}")
            .WithColor(color == default ? Color.Gold : color)
            .WithImageUrl(game.GetTablePicUrl())
            .AddField("Nagyobb", $"Szorzó: **{game.HighMultiplier.ToString()}**\n" +
                                 $"Nyeremény: **{game.HighStake.ToString()} kredit**", true)
            .AddField("Kisebb", $"Szorzó: **{game.LowMultiplier.ToString()}**\n" +
                                $"Nyeremény: **{game.LowStake.ToString()}** kredit", true)
            .Build();
    }
    
    public static Embed CrashEmbed(this EmbedBuilder builder, CrashGame game, string desc = null, Color color = default)
    {
        return builder.WithTitle($"Crash - {game.Id}")
            .WithDescription($"Tét: **{game.Bet} kredit**\n{desc}")
            .WithColor(color == default ? Color.Gold : color)
            .AddField("Szorzó", $"`{game.Multiplier:0.0}x`", true)
            .AddField("Profit", $"`{game.Profit:0}`", true)
            .Build();
    }
    
    public static Embed LeaveEmbed(this EmbedBuilder builder, IVoiceChannel vChannel)
    {
        return builder.WithAuthor("SIKERES ELHAGYÁS", SuccessIcon)
            .WithDescription($"A következő csatornából: {vChannel.Mention}")
            .WithColor(Color.Green)
            .Build();
    }

    public static Embed MoveEmbed(this EmbedBuilder builder, IVoiceChannel vChannel)
    {
        return builder.WithAuthor("SIKERES MOZGATÁS", SuccessIcon)
            .WithDescription($"A következő csatornába: {vChannel.Mention}")
            .WithColor(Color.Green)
            .Build();
    }

    public static Embed NowPlayingEmbed(this EmbedBuilder builder, SocketUser user, MusicPlayer player)
    {
        builder.WithAuthor("MOST JÁTSZOTT", PlayingGif)
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
            .AddField("🎶 Várólistán", $"`{player.Queue.Count.ToString()}`", true);
        return builder.Build();
    }

    public static Embed VolumeEmbed(this EmbedBuilder builder, MusicPlayer player)
    {
        return builder.WithAuthor($"HANGERŐ {player.Volume.ToString()}%-RA ÁLLÍTVA", SuccessIcon)
            .WithDescription($"Ebben a csatornában: {player.VoiceChannel.Mention}")
            .WithColor(Color.Green)
            .Build();
    }

    public static Embed QueueEmbed(this EmbedBuilder builder, MusicPlayer player, bool cleared = false)
    {
        builder.WithAuthor(cleared ? "LEJÁTSZÁSI LISTA TÖRÖLVE" : "LEJÁTSZÁSI LISTA LEKÉRVE", SuccessIcon)
            .WithDescription($"Ebben a csatornában: {player.VoiceChannel.Mention}")
            .WithColor(Color.Green);
        if (cleared)
        {
            return builder.Build();
        }
        if (player.Queue.Count == 0)
        {
            builder.WithDescription("`Nincs zene a lejátszási listában`");
        }
        else
        {
            var desc = new StringBuilder();
            foreach (var track in player.Queue)
            {
                desc.AppendLine(//
                    $":{(player.Queue.TakeWhile(n => n != track).Count() + 1).ToWords()}: [`{track.Title}`]({track.Source}) | Hozzáadta: {((MusicPlayer.TrackContext)track.Context)!.AddedBy.Mention}");
            }

            builder.WithDescription(desc.ToString());
        }
        return builder.Build();
    }

    public static Embed AddedToQueueEmbed(this EmbedBuilder builder, List<LavalinkTrack> tracks)
    {
        var desc = tracks.Take(10).Aggregate("", (current, track) => current + $"{tracks.TakeWhile(n => n != track).Count() + 1}. [`{track.Title}`]({track.Source})\n");
        if (tracks.Count > 10)
        {
            desc += $"és még {(tracks.Count - 10).ToString()} zene\n";
        }
        return builder.WithAuthor($"{tracks.Count} SZÁM HOZZÁADVA A VÁRÓLISTÁHOZ", SuccessIcon)
            .WithColor(Color.Orange)
            .WithDescription(desc)
            .Build();
    }

    public static Embed ErrorEmbed(this EmbedBuilder builder, string exception)
    {
        return builder.WithAuthor("HIBA", ErrorIcon)
            .WithTitle("Kérlek próbáld meg újra!")
            .WithColor(Color.Red)
            .AddField("Hibaüzenet", $"```{exception}```")
            .Build();
    }
}