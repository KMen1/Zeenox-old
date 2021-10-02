using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace KBot.Commands
{
    public class Admin : ModuleBase<SocketCommandContext>
    {
        [Command("clear")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task ClearMsg(int amount)
        {
            await Context.Message.DeleteAsync();
            if (amount > 50)
            {
                var eb = new EmbedBuilder
                {
                    Title = "Cannot purge more than 50 messages at one time",
                    Color = Color.Red
                };
                await ReplyAsync(embed: eb.Build());
                return;
            }
            var messages = await Context.Channel.GetMessagesAsync(Context.Message, Direction.Before, amount).FlattenAsync();
            await (Context.Channel as ITextChannel).DeleteMessagesAsync(messages);

            var embed = new EmbedBuilder
            {
                Title = $"Purged {amount} messages",
                Color = Color.Green
            };
            var msg = await ReplyAsync(embed: embed.Build());
            await Task.Delay(5000);
            await msg.DeleteAsync();
        }
        [Command("changenick")]
        [RequireUserPermission(GuildPermission.ChangeNickname)]
        public async Task SetNickAsync([Remainder]string nickname)
        {
            foreach (var user in Context.Guild.Users)
            {
                if (user.Id == Context.Guild.OwnerId)
                {
                    await ReplyAsync($"**Nem sikerült becenevet állítani neki:** `{user.Username}`");
                }
                else
                {
                    await user.ModifyAsync(x => x.Nickname = nickname);
                }
            }
            await ReplyAsync($"**Mostantól mindenki beceneve -> ** `{nickname}`");
        }
        [Command("revnick")]
        [RequireUserPermission(GuildPermission.ChangeNickname)]
        public async Task RevNickChangeAsync()
        {
            foreach (var user in Context.Guild.Users)
            {
                if (user.Id == Context.Guild.OwnerId | user.Id == 132797923049209856)
                {
                    await ReplyAsync("die");
                }
                else
                {
                    await user.ModifyAsync(x => x.Nickname = null);
                }
            }
        }
    }
}
