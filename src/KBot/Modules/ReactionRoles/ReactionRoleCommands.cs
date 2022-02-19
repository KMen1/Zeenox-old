using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.ReactionRoles;

[Group("rr", "Reaction Roles")]
public class ReactionRoleCommands : KBotModuleBase
{
#pragma warning disable AsyncFixer01
    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("create", "RR menü megnyitása.")]
    public async Task AddReactionRoleAsync(string description)
    {
        await DeferAsync().ConfigureAwait(false);
        var embed = new EmbedBuilder()
            .WithTitle("Reaction Roles")
            .WithDescription($"Menü a reaction role-ok beállításához.\n{description}")
            .WithColor(Color.Blue)
            .Build();
        var comp = new ComponentBuilder()
            .WithButton("Hozzáadás", "rradd", ButtonStyle.Success,new Emoji("➕"))
            .WithButton("Eltávolítás", "rrremove", ButtonStyle.Danger, new Emoji("➖"))
            .WithButton("Mentés", "rrsave", emote: new Emoji("💾"))
            .Build();

        await FollowupAsync(embed: embed, components: comp).ConfigureAwait(false);
    }
#pragma warning restore AsyncFixer01
}