using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.Suggestions;

[Group("suggestion", "Ötletekkel kapcsolatos parancsok")]
public class SuggestionCommands : KBotModuleBase
{
    [SlashCommand("create", "Ötlet létrehozása")]
    public async Task CreateSuggestionAsync(string description)
    {
        await DeferAsync().ConfigureAwait(false);

        var embed = new EmbedBuilder()
            .WithAuthor(Context.User.Username, Context.User.GetAvatarUrl())
            .WithTitle("Ötlet")
            .WithDescription(description)
            .WithColor(Color.Blue)
            .Build();
        var comp = new ComponentBuilder()
            .WithButton("Elfogadás", $"suggest-accept:{Context.User.Id}", ButtonStyle.Success, new Emoji("✅"))
            .WithButton("Elutasítás", $"suggest-decline:{Context.User.Id}", ButtonStyle.Danger, new Emoji("❌"))
            .Build();

        var config = await GetGuildConfigAsync().ConfigureAwait(false);
        if (!config.Suggestions.Enabled)
        {
            await FollowupAsync("Ezen a szerveren az ötletek nincsenek engedélyezve.").ConfigureAwait(false);
            return;
        }
        var suggestionChannel = Context.Guild.GetTextChannel(config.Suggestions.AnnouncementChannelId);
        await suggestionChannel.SendMessageAsync(embed: embed, components: comp).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Green, "Ötlet létrehozva", $"Ebben a csatornában: {suggestionChannel.Mention}").ConfigureAwait(false);
    }
}