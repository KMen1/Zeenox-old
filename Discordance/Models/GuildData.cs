using System.Collections.Generic;
using System.Linq;

namespace Discordance.Models;

public class GuildData
{
    public GuildData(IEnumerable<ulong> roleIds)
    {
        Level = 1;
        Xp = 0;
        RoleIds = roleIds.ToList();
        WarnIds = new List<string>();
    }

    public int Level { get; set; }
    public int Xp { get; set; }
    public List<ulong> RoleIds { get; set; }
    public List<string> WarnIds { get; set; }
}