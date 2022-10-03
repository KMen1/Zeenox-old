namespace Discordance.Models;

public class LevelRole
{
    public LevelRole(ulong id, int level)
    {
        Level = level;
        Id = id;
    }

    public ulong Id { get; set; }
    public int Level { get; set; }
}