using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Enums;

namespace KBot.Modules.Utility;

public class Activity : KBotModuleBase
{
    [SlashCommand("activity", "Discord tevékenység elindítása a megadott hangcsatornában!")]
    public async Task ActivityAsync([Summary("csatorna", "Az a csatorna amiben menjen az activity")]IVoiceChannel vChannel, DefaultApplications activity)
    {
        var invite = await vChannel.CreateInviteToApplicationAsync(activity);
        await RespondWithEmbedAsync(EmbedResult.Success, activity.ToString(), $"Kattins a címre az activity elindításához a {vChannel.Name} hangcsatornában!", invite.Url);
    }
}