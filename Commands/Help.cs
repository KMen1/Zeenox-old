using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KBot.Commands
{
    public class Help : ModuleBase<SocketCommandContext>
    {
        public CommandService _commandService;
        public Help(CommandService commandService)
        {
            _commandService = commandService;
        }

        [Command("help"), Alias("h"), Summary("Elküldi az összes elérhető parancsot")]
        public async Task HelpAsync([Remainder]string optCommand = "")
        {
            List<CommandInfo> commands = _commandService.Commands.ToList();

            var list = new List<CommandInfo>();
            foreach (CommandInfo command in commands)
            {
                list.Add(command);
            }
            var commandsname = list.Select(x => x.Name);
            string combindedString = string.Join(", ", commandsname.ToArray());

            EmbedBuilder eb = new EmbedBuilder
            {
                Title = "**Elérhető parancsok**",
                Description = "Összes parancs listázása ( help <parancs> )-al több információt tudhatsz meg az adott parancsról!",
                Color = Color.Orange,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(1575900037337),
                ThumbnailUrl = Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto)
            };

            if (!optCommand.Equals(""))
            {
                var co = list.FirstOrDefault(x => x.Name.ToLower() == optCommand.ToLower());

                if (co.Name == optCommand.ToLower())
                {
                    eb.WithTitle($"**{co.Name.First().ToString().ToUpper() + co.Name[1..]}**");
                    eb.WithDescription($"`{co.Summary}`");
                    string ali = string.Join(", ", co.Aliases);
                    ali.Replace(co.Name, "");
                    eb.AddField("Röviden", ali.Replace(co.Name + ", ", ""));
                }
                else
                {
                    await ReplyAsync($"Nincs ilyen parancs - `{optCommand}`");
                }
            
            }
            else
            {
                eb.WithFooter(footer => footer.WithText($"Requested by: {Context.User.Username}").WithIconUrl(Context.User.GetAvatarUrl(ImageFormat.Auto)));
                eb.AddField("Parancsok", $"`{combindedString}`");
            }

            if (Context.Channel is SocketDMChannel)
            {
                await Context.User.SendMessageAsync(null, false, eb.Build());
            }
            else
            {
                await Context.User.SendMessageAsync(null, false, eb.Build());
                await ReplyAsync($":exclamation: **{Context.User.Mention}** Nézd meg a privát üzeneteidet :exclamation: ");
            }
        }
    }
}
