using System;
using System.Collections.Generic;
using System.Linq;
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
using Lavalink4NET.Player;

namespace KBot.Extensions;

public static class EmbedBuilderExtensions
{
    private const string SuccessIcon = "https://i.ibb.co/HdqsDXh/tick.png";
    private const string ErrorIcon = "https://i.ibb.co/SrZZggy/x.png";
    private const string PlayingGif = "https://bestanimations.com/media/discs/895872755cd-animated-gif-9.gif";

    public static Embed BlackJackEmbed(this EmbedBuilder builder, BlackJackGame game, string desc = null,
        Color color = default)
    {
        return builder.WithTitle($"Blackjack | {game.Id}")
            .WithDescription($"Bet: **{game.Bet}**\n{desc}")
            .WithColor(color == default ? Color.Gold : color)
            .WithImageUrl(game.GetTablePicUrl())
            .AddField("Player", $"Value: `{game.PlayerScore.ToString()}`", true)
            .AddField("Dealer", game.Hidden ? "Value: `?`" : $"Value: `{game.DealerScore.ToString()}`", true)
            .Build();
    }

    public static Embed HighLowEmbed(this EmbedBuilder builder, HighLowGame game, string desc = null,
        Color color = default)
    {
        return builder.WithTitle($"Higher/Lower | {game.Id}")
            .WithDescription($"Bet: **{game.Stake}**\n{desc}")
            .WithColor(color == default ? Color.Gold : color)
            .WithImageUrl(game.GetTablePicUrl())
            .AddField("Higher", $"Multiplier: **{game.HighMultiplier.ToString()}**\n" +
                                $"Prize: **{game.HighStake.ToString()}**", true)
            .AddField("Lower", $"Multiplier: **{game.LowMultiplier.ToString()}**\n" +
                               $"Prize: **{game.LowStake.ToString()}**", true)
            .Build();
    }

    public static Embed CrashEmbed(this EmbedBuilder builder, CrashGame game, string desc = null, Color color = default)
    {
        return builder.WithTitle($"Crash | {game.Id}")
            .WithDescription($"Bet: **{game.Bet}**\n{desc}")
            .WithColor(color == default ? Color.Gold : color)
            .AddField("Multiplier", $"`{game.Multiplier:0.00}x`", true)
            .AddField("Profit", $"`{game.Profit:0}`", true)
            .Build();
    }

    public static Embed TowersEmbed(this EmbedBuilder builder, TowersGame game, string desc = "", Color color = default)
    {
        return builder.WithTitle($"Towers | {game.Id}")
            .WithDescription($"Bet: **{game.Bet}**\nDifficulty: **{game.Difficulty.GetDescription()}**\n{desc}")
            .WithColor(color == default ? Color.Gold : color)
            .Build();
    }

    public static Embed LeaveEmbed(this EmbedBuilder builder, IVoiceChannel vChannel)
    {
        return builder.WithAuthor("SUCCESS", SuccessIcon)
            .WithDescription($"Left {vChannel.Mention}")
            .WithColor(Color.Green)
            .Build();
    }

    public static Embed MoveEmbed(this EmbedBuilder builder, IVoiceChannel vChannel)
    {
        return builder.WithAuthor("SUCCESS", SuccessIcon)
            .WithDescription($"Moved to {vChannel.Mention}")
            .WithColor(Color.Green)
            .Build();
    }

    public static Embed NowPlayingEmbed(this EmbedBuilder builder, MusicPlayer player)
    {
        builder.WithAuthor("NOW PLAYING", PlayingGif)
            .WithTitle(player.CurrentTrack!.Title)
            .WithUrl(player.CurrentTrack.Source)
            .WithImageUrl($"https://img.youtube.com/vi/{player.CurrentTrack.TrackIdentifier}/maxresdefault.jpg")
            .WithColor(Color.Green)
            .AddField("👨 Added by", player.LastRequestedBy.Mention, true)
            .AddField("🔼 Uploader", $"`{player.CurrentTrack.Author}`", true)
            .AddField("🎙️ Channel", player.VoiceChannel.Mention, true)
            .AddField("🕐 Length", $"`{player.CurrentTrack.Duration.ToString("c")}`", true)
            .AddField("🔁 Loop", player.Loop ? "`On`" : "`Off`", true)
            .AddField("🔁 Autoplay", player.AutoPlay ? "`On`" : "`Off`", true)
            .AddField("🔊 Volume", $"`{Math.Round(player.Volume * 100).ToString()}%`", true)
            .AddField("📝 Filter", player.FilterEnabled is not null ? $"`{player.FilterEnabled}`" : "`None`", true)
            .AddField("🎶 In Queue", $"`{player.QueueCount.ToString()}`", true)
            .AddField("⏭ Voteskip", $"`{player.SkipVotes.Count.ToString()}/{player.SkipVotesNeeded.ToString()}`", true);
        return builder.Build();
    }

    public static Embed VolumeEmbed(this EmbedBuilder builder, MusicPlayer player)
    {
        return builder.WithAuthor($"VOLUME SET TO {player.Volume.ToString()}%", SuccessIcon)
            .WithDescription($"In channel {player.VoiceChannel.Mention}")
            .WithColor(Color.Green)
            .Build();
    }

    public static Embed QueueEmbed(this EmbedBuilder builder, MusicPlayer player, bool cleared = false)
    {
        builder.WithAuthor(cleared ? "QUEUE CLEARED" : "CURRENT QUEUE", SuccessIcon)
            .WithDescription($"In channel {player.VoiceChannel.Mention}")
            .WithColor(Color.Green);
        if (cleared) return builder.Build();
        if (player.QueueCount == 0)
        {
            builder.WithDescription("`No songs in queue`");
        }
        else
        {
            var desc = new StringBuilder();
            foreach (var track in player.Queue)
                desc.AppendLine( //
                    $":{(player.Queue.TakeWhile(n => n != track).Count() + 1).ToWords()}: [`{track.Title}`]({track.Source}) | Added by: {((TrackContext) track.Context)!.AddedBy.Mention}");

            builder.WithDescription(desc.ToString());
        }

        return builder.Build();
    }

    public static Embed AddedToQueueEmbed(this EmbedBuilder builder, IEnumerable<LavalinkTrack> tracks)
    {
        var enumerable = tracks.ToList();
        var desc = enumerable.Take(10).Aggregate("",
            (current, track) =>
                current + $"{enumerable.TakeWhile(n => n != track).Count() + 1}. [`{track.Title}`]({track.Source})\n");
        if (enumerable.Count > 10) desc += $"and {(enumerable.Count - 10).ToString()} more\n";
        return builder.WithAuthor($"{enumerable.Count} TRACKS ADDED TO QUEUE", SuccessIcon)
            .WithColor(Color.Orange)
            .WithDescription(desc)
            .Build();
    }

    public static Embed ErrorEmbed(this EmbedBuilder builder, string exception)
    {
        return builder.WithAuthor("ERROR", ErrorIcon)
            .WithTitle("Please try again!")
            .WithColor(Color.Red)
            .AddField("Message", $"```{exception}```")
            .Build();
    }
}