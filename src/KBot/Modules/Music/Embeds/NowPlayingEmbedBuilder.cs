using System;
using System.Globalization;
using Discord;

namespace KBot.Modules.Music.Embeds;

public class NowPlayingEmbedBuilder : EmbedBuilder
{
    public NowPlayingEmbedBuilder(MusicPlayer player)
    {
        Author = new EmbedAuthorBuilder
        {
            Name = "NOW PLAYING",
            IconUrl = "https://bestanimations.com/media/discs/895872755cd-animated-gif-9.gif"
        };
        Title = player.CurrentTrack!.Title;
        Url = player.CurrentTrack.Source;
        ImageUrl =
            $"https://img.youtube.com/vi/{player.CurrentTrack.TrackIdentifier}/maxresdefault.jpg";
        Color = new Color(31, 31, 31);
        Fields.Add(
            new EmbedFieldBuilder
            {
                Name = "👤 Added by",
                Value = player.LastRequestedBy.Mention,
                IsInline = true
            }
        );
        Fields.Add(
            new EmbedFieldBuilder
            {
                Name = "🎙️ Channel",
                Value = player.VoiceChannel.Mention,
                IsInline = true
            }
        );
        Fields.Add(
            new EmbedFieldBuilder
            {
                Name = "🕐 Length",
                Value =
                    $"`{player.CurrentTrack.Duration.ToString("c", CultureInfo.InvariantCulture)}`",
                IsInline = true
            }
        );
        Fields.Add(
            new EmbedFieldBuilder
            {
                Name = "🔎 Autoplay",
                Value = player.AutoPlay ? "`On`" : "`Off`",
                IsInline = true
            }
        );
        Fields.Add(
            new EmbedFieldBuilder
            {
                Name = "🔁 Loop",
                Value = player.Loop ? "`On`" : "`Off`",
                IsInline = true
            }
        );
        Fields.Add(
            new EmbedFieldBuilder
            {
                Name = "🔊 Volume",
                Value =
                    $"`{Math.Round(player.Volume * 100).ToString(CultureInfo.InvariantCulture)}%`",
                IsInline = true
            }
        );
        Fields.Add(
            new EmbedFieldBuilder
            {
                Name = "📝 Filter",
                Value = player.FilterEnabled is not null ? $"`{player.FilterEnabled}`" : "`None`",
                IsInline = true
            }
        );
        Fields.Add(
            new EmbedFieldBuilder
            {
                Name = "🎶 In Queue",
                Value = $"`{player.QueueCount.ToString(CultureInfo.InvariantCulture)}`",
                IsInline = true
            }
        );
        Fields.Add(
            new EmbedFieldBuilder
            {
                Name = "⏭ Voteskip",
                Value =
                    $"`{player.SkipVotes.Count.ToString(CultureInfo.InvariantCulture)}/{player.SkipVotesNeeded.ToString(CultureInfo.InvariantCulture)}`",
                IsInline = true
            }
        );
    }
}
