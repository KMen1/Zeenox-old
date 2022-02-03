using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.ReactionRoles;

[Group("rr", "Reaction Roles")]
public class ReactionRoleCommands : KBotModuleBase
{
#pragma warning disable AsyncFixer01
    [SlashCommand("create", "RR menü megnyitása.")]
    public async Task AddReactionRoleAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var embed = new EmbedBuilder
        {
            Title = "Reaction Roles",
            Description = "Menü a reaction role-ok beállításához.",
            Color = Color.Green
        }.Build();
        var comp = new ComponentBuilder()
            .WithButton("Hozzáadás", "rradd", ButtonStyle.Success,new Emoji("➕"))
            .WithButton("Eltávolítás", "rrremove", ButtonStyle.Danger, new Emoji("➖"))
            .WithButton("Mentés", "rrsave", emote: new Emoji("💾"))
            .Build();

        await Context.Channel.SendMessageAsync(embed: embed, components: comp).ConfigureAwait(false);
    }
#pragma warning restore AsyncFixer01
}