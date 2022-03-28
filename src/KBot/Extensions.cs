using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Discord;
using Discord.WebSocket;
using Humanizer;
using KBot.Enums;
using KBot.Modules.Gambling.BlackJack;
using KBot.Modules.Gambling.Crash;
using KBot.Modules.Gambling.HighLow;
using KBot.Modules.Gambling.Towers;
using KBot.Modules.Music;
using Lavalink4NET.Filters;
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
        if (name == null) return null;
        var field = type.GetField(name);
        if (field == null) return null;
        if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr)
        {
            return attr.Description;
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
        return builder.WithTitle($"Blackjack | {game.Id}")
            .WithDescription($"Tét: **{game.Stake} kredit**\n{desc}")
            .WithColor(color == default ? Color.Gold : color)
            .WithImageUrl(game.GetTablePicUrl())
            .AddField("Játékos", $"Érték: `{game.PlayerScore.ToString()}`", true)
            .AddField("Osztó", game.Hidden ? "Érték: `?`" : $"Érték: `{game.DealerScore.ToString()}`", true)
            .Build();
    }
    
    public static Embed HighLowEmbed(this EmbedBuilder builder, HighLowGame game, string desc = null, Color color = default)
    {
        return builder.WithTitle($"Higher/Lower | {game.Id}")
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
        return builder.WithTitle($"Crash | {game.Id}")
            .WithDescription($"Tét: **{game.Bet} kredit**\n{desc}")
            .WithColor(color == default ? Color.Gold : color)
            .AddField("Szorzó", $"`{game.Multiplier:0.00}x`", true)
            .AddField("Profit", $"`{game.Profit:0}`", true)
            .Build();
    }

    public static Embed TowersEmbed(this EmbedBuilder builder, TowersGame game, string desc = "", Color color = default)
    {
        return builder.WithTitle($"Towers | {game.Id}")
            .WithDescription($"Tét: **{game.Bet} kredit**\nNehézség: **{game.Difficulty.GetDescription()}**\nKilépéshez: `/towers stop {game.Id}`\n{desc}")
            .WithColor(color == default ? Color.Gold : color)
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

    public static Embed NowPlayingEmbed(this EmbedBuilder builder, MusicPlayer player)
    {
        builder.WithAuthor("MOST JÁTSZOTT", PlayingGif)
            .WithTitle(player.CurrentTrack.Title)
            .WithUrl(player.CurrentTrack.Source)
            .WithImageUrl($"https://img.youtube.com/vi/{player.CurrentTrack.TrackIdentifier}/maxresdefault.jpg")
            .WithColor(Color.Green)
            .AddField("👨 Hozzáadta", player.LastRequestedBy.Mention, true)
            .AddField("🔼 Feltöltötte", $"`{player.CurrentTrack.Author}`", true)
            .AddField("🎙️ Csatorna", player.VoiceChannel.Mention, true)
            .AddField("🕐 Hosszúság", $"`{player.CurrentTrack.Duration.ToString("c")}`", true)
            .AddField("🔁 Ismétlés", player.LoopEnabled ? "`Igen`" : "`Nem`", true)
            .AddField("🔊 Hangerő", $"`{Math.Round(player.Volume * 100).ToString()}%`", true)
            .AddField("📝 Szűrő", player.FilterEnabled is not null ? $"`{player.FilterEnabled}`" : "`Nincs`", true)
            .AddField("🎶 Várólistán", $"`{player.QueueCount.ToString()}`", true);
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
        if (player.QueueCount == 0)
        {
            builder.WithDescription("`Nincs zene a lejátszási listában`");
        }
        else
        {
            var desc = new StringBuilder();
            foreach (var track in player.QueueList)
            {
                desc.AppendLine(//
                    $":{(player.QueueList.TakeWhile(n => n != track).Count() + 1).ToWords()}: [`{track.Title}`]({track.Source}) | Hozzáadta: {((TrackContext)track.Context)!.AddedBy.Mention}");
            }

            builder.WithDescription(desc.ToString());
        }
        return builder.Build();
    }

    public static Embed AddedToQueueEmbed(this EmbedBuilder builder, IEnumerable<LavalinkTrack> tracks)
    {
        var enumerable = tracks.ToList();
        var desc = enumerable.Take(10).Aggregate("", (current, track) => current + $"{enumerable.TakeWhile(n => n != track).Count() + 1}. [`{track.Title}`]({track.Source})\n");
        if (enumerable.Count > 10)
        {
            desc += $"és még {(enumerable.Count - 10).ToString()} zene\n";
        }
        return builder.WithAuthor($"{enumerable.Count} SZÁM HOZZÁADVA A VÁRÓLISTÁHOZ", SuccessIcon)
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

    public static string GetGradeEmoji(this Grade grade)
    {
        return grade switch
        {
            Grade.N => "<:osuF:936588252763271168>",
            Grade.F => "<:osuF:936588252763271168>",
            Grade.D => "<:osuD:936588252884910130>",
            Grade.C => "<:osuC:936588253031723078>",
            Grade.B => "<:osuB:936588252830380042>",
            Grade.A => "<:osuA:936588252754882570>",
            Grade.S => "<:osuS:936588252872318996>",
            Grade.SH => "<:osuSH:936588252834574336>",
            Grade.X => "<:osuX:936588252402573333>",
            Grade.XH => "<:osuXH:936588252822007818>",
            _ => "<:osuF:936588252763271168>"
        };
    }
    public static Color GetGradeColor(this Grade grade)
    {
        return grade switch
        {
            Grade.N => Color.Default,
            Grade.F => new Color(109, 73, 38),
            Grade.D => Color.Red,
            Grade.C => Color.Purple,
            Grade.B => Color.Blue,
            Grade.A => Color.Green,
            Grade.S => Color.Gold,
            Grade.SH => Color.LightGrey,
            Grade.X => Color.Gold,
            Grade.XH => Color.LightGrey,
            _ => Color.Default
        };
    }

    public static double NextDouble(this RandomNumberGenerator generator, double minimumValue, double maximumValue)
    {
        var randomNumber = new byte[1];
        generator.GetBytes(randomNumber);
        var multiplier = Math.Max(0, (randomNumber[0] / 255d) - 0.00000000001d);
        var range = maximumValue - minimumValue + 1;
        var randomValueInRange = Math.Floor(multiplier * range);
        return minimumValue + randomValueInRange;
    }
    
    public static string EnableBassBoost(this PlayerFilterMap map)
    {
        map.Equalizer = new EqualizerFilterOptions
        {
            Bands = new EqualizerBand[]
            {
                new(0, 0.2f),
                new(1, 0.2f),
                new(2, 0.2f)
            }
        };
        return "Basszus Erősítés";
    }

    public static string EnablePop(this PlayerFilterMap map)
    {
        map.Equalizer = new EqualizerFilterOptions
        {
            Bands = new EqualizerBand[]
            {
                new(0, 0.65f),
                new(1, 0.45f),
                new(2, -0.25f),
                new(3, -0.25f),
                new(4, -0.25f),
                new(5, 0.45f),
                new(6, 0.55f),
                new(7, 0.6f),
                new(8, 0.6f),
                new(9, 0.6f),
            }
        };
        return "Pop";
    }
    public static string EnableSoft(this PlayerFilterMap map)
    {
        map.Equalizer = new EqualizerFilterOptions
        {
            Bands = new EqualizerBand[]
            {
                new(8, -0.25f),
                new(9, -0.25f),
                new(10, -0.25f),
                new(11, -0.25f),
                new(12, -0.25f),
                new(13, -0.25f)
            }
        };
        return "Pop";
    }
    public static string EnableTreblebass(this PlayerFilterMap map)
    {
        map.Equalizer = new EqualizerFilterOptions
        {
            Bands = new EqualizerBand[]
            {
                new(0, 0.6f),
                new(1, 0.67f),
                new(2, 0.67f),
                new(4, -0.2f),
                new(5, 0.15f),
                new(6, -0.25f),
                new(7, 0.23f),
                new(8, 0.35f),
                new(9, 0.45f),
                new(10, 0.55f),
                new(11, 0.6f),
                new(12, 0.55f),
            }
        };
        return "Pop";
    }

    public static string EnableNightcore(this PlayerFilterMap map)
    {
        map.Timescale = new TimescaleFilterOptions
        {
            Speed = 1.165f,
            Pitch = 1.125f,
            Rate = 1.05f
        };
        return "Nightcore";
    }

    public static string EnableEightd(this PlayerFilterMap map)
    {
        map.Rotation = new RotationFilterOptions
        {
            Frequency = 0.2f
        };
        return "8D";
    }

    public static string EnableVaporwave(this PlayerFilterMap map)
    {
        map.Timescale = new TimescaleFilterOptions
        {
            Speed = 1.0f,
            Pitch = 0.5f,
            Rate = 1.0f
        };
        return "Vaporwave";
        /*
new TremoloFilter()
{
    Depth = 0.3,
    Frequency = 14
}*/
    }

    public static string EnableDoubletime(this PlayerFilterMap map)
    {
        map.Timescale = new TimescaleFilterOptions
        {
            Speed = 1.0f,
            Pitch = 0.5f,
            Rate = 1.0f
        };
        return "Gyorsítás";
    }
    
    public static string EnableSlowmotion(this PlayerFilterMap map)
    {
        map.Timescale = new TimescaleFilterOptions
        {
            Speed = 0.5f,
            Pitch = 1.0f,
            Rate = 0.8f
        };
        return "Lassítás";
    }
    public static string EnableChipmunk(this PlayerFilterMap map)
    {
        map.Timescale = new TimescaleFilterOptions
        {
            Speed = 1.05f,
            Pitch = 1.35f,
            Rate = 1.25f
        };
        return "Alvin és a mókusok";
    }
    public static string EnableDarthvader(this PlayerFilterMap map)
    {
        map.Timescale = new TimescaleFilterOptions
        {
            Speed = 0.975f,
            Pitch = 0.5f,
            Rate = 0.8f
        };
        return "Darth Vader";
    }
    public static string EnableDance(this PlayerFilterMap map)
    {
        map.Timescale = new TimescaleFilterOptions
        {
            Speed = 1.25f,
            Pitch = 1.25f,
            Rate = 1.25f
        };
        return "Tánc";
    }
    public static string EnableChina(this PlayerFilterMap map)
    {
        map.Timescale = new TimescaleFilterOptions
        {
            Speed = 0.75f,
            Pitch = 1.25f,
            Rate = 1.25f
        };
        return "Kína";
    }
    public static string EnableVibrato(this PlayerFilterMap map)
    {
        map.Vibrato = new VibratoFilterOptions
        {
            Frequency = 4.0f,
            Depth = 0.75f
        };
        return "Vibrato";
    }

    public static string EnableTremolo(this PlayerFilterMap map)
    {
        map.Tremolo = new TremoloFilterOptions
        {
            Frequency = 4.0f,
            Depth = 0.75f
        };
        return "Tremolo";
    }
    
    /*public static IEnumerable<IFilter> Vibrate()
{
    return new IFilter[]
    {
        new VibratoFilter
        {
            Frequency = 4.0,
            Depth = 0.75
        },
        new TremoloFilter
        {
            Frequency = 4.0,
            Depth = 0.75
        }
    };
}*/

    public static MessageComponent NowPlayerComponents(this ComponentBuilder builder, MusicPlayer player)
    {
        return builder
            .WithButton(" ", "previous", emote: new Emoji("⏮"), disabled: !player.CanGoBack, row: 0)
            .WithButton(" ", "pause", emote: player.State == PlayerState.Playing ? new Emoji("⏸") : new Emoji("▶"), row: 0)
            .WithButton(" ", "stop", emote: new Emoji("⏹"), row: 0, style: ButtonStyle.Danger)
            .WithButton(" ", "next", emote: new Emoji("⏭"), disabled: !player.CanGoForward, row: 0)
            .WithButton(" ", "volumedown", emote: new Emoji("🔉"), row: 1, disabled: player.Volume == 0)
            .WithButton(" ", "repeat", emote: new Emoji("🔁"), row: 1)
            .WithButton(" ", "clearfilters", emote: new Emoji("🗑️"), row: 1)
            .WithButton(" ", "volumeup", emote: new Emoji("🔊"), row: 1, disabled: player.Volume == 1.0f)
            .WithSelectMenu(new SelectMenuBuilder()
                .WithPlaceholder("Szűrő kiválasztása")
                .WithCustomId("filterselectmenu")
                .WithMinValues(1)
                .WithMaxValues(1)
                .AddOption("Basszus Erősítés", "bassboost")
                .AddOption("Pop", "pop")
                .AddOption("Lágy", "soft")
                .AddOption("Hangos", "treblebass")
                .AddOption("Nightcore", "nightcore")
                .AddOption("8D", "eightd")
                .AddOption("Kínai", "china")
                .AddOption("Vaporwave", "vaporwave")
                .AddOption("Gyorsítás", "doubletime")
                .AddOption("Lassítás", "slowmotion")
                .AddOption("Alvin és a mókusok", "chipmunk")
                .AddOption("Darthvader", "darthvader")
                .AddOption("Tánc", "dance")
                .AddOption("Vibrato hanghatás", "vibrato")
                .AddOption("Tremolo hanghatás", "tremolo"), 2)
            .Build();
    }
}