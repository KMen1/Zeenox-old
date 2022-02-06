using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Modules.Setup.Helpers;

namespace KBot.Modules.Setup;

public class SetupComponents : KBotModuleBase
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [ComponentInteraction("setupselect")]
    public async Task SetupSelectMenu(params string[] selection)
    {
        await DeferAsync().ConfigureAwait(false);
        var moduleName = selection[0].Split(":")[0];
        var moduleEnum = Enum.Parse<GuildModules>(moduleName);
        var selectedproperty = selection[0].Split(":")[1];

        var config = await GetGuildConfigAsync().ConfigureAwait(false);
        var module = Converters.GetModuleConfigFromGuildConfig(moduleEnum, config);

        var msg = await Context.Interaction.GetOriginalResponseAsync().ConfigureAwait(false);
        var embed = msg.Embeds.First().ToEmbedBuilder();

        var property = module.GetType().GetProperty(selectedproperty);
        var value = property!.GetValue(module);
        if (value is bool enabled)
        {
            property.SetValue(module, !enabled);
            embed.Fields.First(x => x.Name == GetTitleFromPropertyName(selectedproperty)).Value = enabled ? "`Nem`" : "`Igen`";
            await Database.SaveGuildConfigAsync(Context.Guild.Id, config).ConfigureAwait(false);
        }
        else
        {
            var reqMsg =
                await FollowupAsync(
                        $"Kérlek pingeld be vagy add meg az id-jét a következőnek: {GetTitleFromPropertyName(property.Name)}")
                    .ConfigureAwait(false);
            var newValue = await InteractiveService.NextMessageAsync(x => x.Channel.Id == Context.Channel.Id && x.Author.Id == Context.User.Id).ConfigureAwait(false);
            if (!newValue.IsSuccess)
            {
                return;
            }
            var id = Convert.ToUInt64(Regex.Replace(newValue.Value!.Content, "[^0-9]", ""));
            property.SetValue(module, id);
            if (Context.Guild.GetChannel(id) is SocketCategoryChannel category)
            {
                embed.Fields.First(x => x.Name == GetTitleFromPropertyName(selectedproperty)).Value = $"`{category.Name}`";
            }
            else if (Context.Guild.GetChannel(id) is SocketTextChannel textChannel)
            {
                embed.Fields.First(x => x.Name == GetTitleFromPropertyName(selectedproperty)).Value = textChannel.Mention;
            }
            else if (Context.Guild.GetChannel(id) is SocketVoiceChannel voiceChannel)
            {
                embed.Fields.First(x => x.Name == GetTitleFromPropertyName(selectedproperty)).Value = voiceChannel.Mention;
            }
            else if (Context.Guild.GetRole(id) is { } role)
            {
                embed.Fields.First(x => x.Name == GetTitleFromPropertyName(selectedproperty)).Value = role.Mention;
            }
            else
            {
                embed.Fields.First(x => x.Name == GetTitleFromPropertyName(selectedproperty)).Value = $"`{id}`";
            }

            await Database.SaveGuildConfigAsync(Context.Guild.Id, config).ConfigureAwait(false);
            await reqMsg.DeleteAsync().ConfigureAwait(false);
        }

        await msg.ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
    }
    private static string GetTitleFromPropertyName(string propertyName)
    {
        return propertyName switch
        {
            "Enabled" => "Bekapcsolva",
            "UserJoinedChannelId" => "Köszöntő csatorna",
            "UserLeftChannelId" => "Kilépő csatorna",
            "UserBannedChannelId" => "Ban csatorna",
            "UserUnbannedChannelId" => "Unban csatorna",
            "CategoryId" => "Kategória",
            "CreateChannelId" => "Létrehozás csatorna",
            "AnnouncementChannelId" => "Bejelentő csatorna",
            "StreamingChannelId" => "Vetítő csatorna",
            "RoleId" => "Rang",
            "PointsToLevelUp" => "Pontok a szintlépéshez",
            _ => "Ismeretlen"
        };
    }
}