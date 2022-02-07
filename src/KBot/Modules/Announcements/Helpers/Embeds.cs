using System;
using Discord;
using Discord.WebSocket;
using KBot.Enums;

namespace KBot.Modules.Announcements.Helpers;

public static class Embeds
{
    private const string SuccessIcon = "https://i.ibb.co/HdqsDXh/tick.png";
    private const string ErrorIcon = "https://i.ibb.co/SrZZggy/x.png";

    public static Embed MovieEventEmbed(SocketGuildEvent movieEvent, EventEmbedType embedType)
    {
        var embed = new EmbedBuilder
        {
            Title = movieEvent.Name,
            Description = movieEvent.Description,
            Timestamp = DateTimeOffset.UtcNow,
            Fields =
            {
                new EmbedFieldBuilder
                {
                    Name = "👨 Létrehozta",
                    Value = movieEvent.Creator.Mention,
                    IsInline = true
                },
                new EmbedFieldBuilder()
                {
                    Name = "🕐 Időpont",
                    Value = movieEvent.StartTime.ToString("yyyy. MM. dd. HH:mm"),
                    IsInline = true
                },
                new EmbedFieldBuilder()
                {
                    Name = "🎙 Csatorna",
                    Value = movieEvent.Channel.Name,
                    IsInline = true
                }
            }
        };
        switch (embedType)
        {
            case EventEmbedType.Scheduled:
            {
                embed.WithAuthor("ÚJ FILM ESEMÉNY ÜTEMEZVE!", SuccessIcon);
                embed.WithColor(Color.Orange);
                break;
            }
            case EventEmbedType.Updated:
            {
                embed.WithAuthor("FILM ESEMÉNY FRISSÍTVE!", SuccessIcon);
                embed.WithColor(Color.Orange);
                break;
            }
            case EventEmbedType.Started:
            {
                embed.WithAuthor("FILM ESEMÉNY KEZDŐDIK!", SuccessIcon);
                embed.WithColor(Color.Green);
                break;
            }
            case EventEmbedType.Cancelled:
            {
                embed.WithAuthor("FILM ESEMÉNY TÖRÖLVE!", ErrorIcon);
                embed.WithColor(Color.Red);
                break;
            }
        }
        return embed.Build();
    }

    public static Embed TourEventEmbed(SocketGuildEvent tourEvent, EventEmbedType tourEmbedType)
    {
        var embed = new EmbedBuilder
        {
            Title = tourEvent.Name,
            Description = tourEvent.Description,
            Timestamp = DateTimeOffset.UtcNow,
            Fields =
            {
                new EmbedFieldBuilder
                {
                    Name = "👨 Létrehozta",
                    Value = tourEvent.Creator.Mention,
                    IsInline = true
                },
                new EmbedFieldBuilder()
                {
                    Name = "🕐 Időpont",
                    Value = tourEvent.StartTime.ToString("yyyy. MM. dd. HH:mm"),
                    IsInline = true
                },
                new EmbedFieldBuilder()
                {
                    Name = "⛺ Helyszín",
                    Value = tourEvent.Location,
                    IsInline = false
                }
            }
        };
        switch (tourEmbedType)
        {
            case EventEmbedType.Scheduled:
            {
                embed.WithAuthor("ÚJ TÚRA ESEMÉNY ÜTEMEZVE!", SuccessIcon);
                embed.WithColor(Color.Orange);
                break;
            }
            case EventEmbedType.Updated:
            {
                embed.WithAuthor("TÚRA ESEMÉNY FRISSÍTVE!", SuccessIcon);
                embed.WithColor(Color.Orange);
                break;
            }
            case EventEmbedType.Started:
            {
                embed.WithAuthor("TÚRA ESEMÉNY KEZDŐDIK!", SuccessIcon);
                embed.WithColor(Color.Green);
                break;
            }
            case EventEmbedType.Cancelled:
            {
                embed.WithAuthor("TÚRA ESEMÉNY TÖRÖLVE!", ErrorIcon);
                embed.WithColor(Color.Red);
                break;
            }
        }
        return embed.Build();
    }
}