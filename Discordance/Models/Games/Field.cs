using Discord;

namespace Discordance.Models.Games;

public struct Field
{
    public Emoji Emoji { get; init; }
    public bool IsClicked { get; init; }
    public bool IsMine { get; init; }
    public string Label { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public bool Disabled { get; init; }
    public int Prize { get; init; }
}
