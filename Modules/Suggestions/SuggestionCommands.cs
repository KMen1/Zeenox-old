using System.Threading.Tasks;
using Discord.Interactions;

namespace KBot.Modules.Suggestions;

[Group("suggestion", "Ötletekkel kapcsolatos parancsok")]
public class SuggestionCommands : KBotModuleBase
{
    [SlashCommand("create", "Ötlet létrehozása")]
    public async Task CreateSuggestionAsync(string descripton)
    {
        
    }
    [SlashCommand("approve", "Ötlet elfogadása (admin)")]
    public async Task ApproveSuggestionAsync(int id, string reason)
    {
        
    }
    [SlashCommand("deny", "Ötlet elutasítása (admin)")]
    public async Task DenySuggestionAsync(int id, string reason)
    {
        
    }
}