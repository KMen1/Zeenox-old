using Discord;

namespace KBot.Modules.Gambling.Tower.Game;

public struct Field
{
    public int X { get; init; }
    public int Y { get; init; }
    public bool IsMine { get; init; }
    public Emoji Emoji { get; init; }
    public string Label { get; init; }
    public int Prize { get; init; }
    public bool Disabled { get; init; }
}