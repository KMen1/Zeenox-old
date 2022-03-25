using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Models;
using KBot.Modules.Setup.Helpers;

namespace KBot.Modules.Setup;

public class SetupCommands : KBotModuleBase
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("setup", "Bot beállítása ehhez a szerverhez")]
    public async Task SetupCommandsAsync(ModuleType module)
    {
        await DeferAsync().ConfigureAwait(false);
        var embed = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = Context.Guild.Name,
                IconUrl = Context.Guild.IconUrl
            },
            Title = Converters.GetTitleFromModuleEnum(module),
            Color = Color.Gold
        };
        var components = new ComponentBuilder();
        var selectMenu = new SelectMenuBuilder()
            .WithPlaceholder("Módosítás...")
            .WithCustomId("setupselect")
            .WithMaxValues(1)
            .WithMinValues(1);
        var config = await GetGuildConfigAsync().ConfigureAwait(false);
        config ??= new GuildConfig();
        var moduleconfig = config.GetModuleConfigFromGuildConfig(module);
        foreach (var property in moduleconfig.GetType().GetProperties())
        {
            var value = property.GetValue(moduleconfig);
            var title = Converters.GetTitleFromPropertyName(property.Name);
            if (value is bool enabled)
            {
                embed.AddField("Bekapcsolva", enabled ? "`Igen`" : "`Nem`");
                selectMenu.AddOption(title, $"{module.ToString()}:{property.Name}");
                continue;
            }

            if (value is List<LevelRole> levelRoles)
            {
                var desc = new StringBuilder();
                if (levelRoles.Count == 0)
                {
                    desc.AppendLine("`Nincs`");
                }
                foreach (var role in levelRoles)
                {
                    desc.AppendLine($"Lvl. {role.Level} - {Context.Guild.GetRole(role.RoleId).Mention}");
                }
                embed.AddField("Auto Rangok", desc.ToString());
            }
            else if (Context.Guild.GetChannel(Convert.ToUInt64(value)) is SocketCategoryChannel category)
            {
                embed.AddField(title, $"`{category.Name}`");
            }
            else if (Context.Guild.GetChannel(Convert.ToUInt64(value)) is SocketTextChannel textChannel)
            {
                embed.AddField(title, textChannel.Mention);
            }
            else if (Context.Guild.GetChannel(Convert.ToUInt64(value)) is SocketVoiceChannel voiceChannel)
            {
                embed.AddField(title, voiceChannel.Mention);
            }
            else if (Context.Guild.GetRole(Convert.ToUInt64(value)) is { } role)
            {
                embed.AddField(title, role.Mention);
            }
            else
            {
                embed.AddField(title, $"`{value}`");
            }

            selectMenu.AddOption(title, $"{module.ToString()}:{property.Name}");
        }

        if (module is ModuleType.Leveling)
        {
            components.WithButton("Auto Rang", "setupaddlevelrole", ButtonStyle.Success, new Emoji("➕"));
            components.WithButton("Auto Rang", "setupremovelevelrole", ButtonStyle.Danger, new Emoji("➖"));
        }
        await FollowupAsync(embed: embed.Build(), components: components.WithSelectMenu(selectMenu).Build()).ConfigureAwait(false);
    }
}