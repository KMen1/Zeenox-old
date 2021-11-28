using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KBot.Commands
{
    public class Chat : ModuleBase<SocketCommandContext>
    {
        [Command("dm"), Alias("msg", "message")]
        [Summary("Üzenetet küld a kiválasztott embernek")]
        public async Task SendDMAsync(IUser user, [Remainder]string message)
        {
            await Context.Message.DeleteAsync();
            IMessageChannel channel = await user.CreateDMChannelAsync();
            var eb = new EmbedBuilder()
            {
                Description = $"Üzenetet kaptál tőle: **@{ Context.User.Username + "#" + Context.User.Discriminator}** \n ```{message}```",
                Color = Color.Orange,
                Timestamp = DateTime.UtcNow,
                Footer = new EmbedFooterBuilder()
                {
                    Text = "Küldte: " + Context.User.Username,
                    IconUrl = Context.User.GetAvatarUrl()
                }
            };
            await channel.SendMessageAsync("", false, eb.Build());
        }
        [Command("clear"), Alias("clr"), Summary("Kitörli az adott mennyiségű üzenetet")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task ClearAsync(int numofmsg)
        {
            try
            {
                IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(numofmsg, CacheMode.AllowDownload).FlattenAsync();
                await ((ITextChannel)Context.Channel).DeleteMessagesAsync(messages);
            }
            catch (Exception e)
            {
                await ReplyAsync(e.Message.ToString());
            }

            var msg = await ReplyAsync($"**Deleted** `{numofmsg}` **messages!**");
            await Task.Delay(2000);
            await msg.DeleteAsync();
        }
    }
}
