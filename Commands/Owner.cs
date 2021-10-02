using Discord.Commands;
using Discord;
using System.Threading.Tasks;
using System.IO;

namespace KBot.Commands
{
    public class Owner : ModuleBase<SocketCommandContext>
    {
        [Command("seticon"), Alias("icon", "si")]
        [RequireOwner]
        public async Task ChangeIconAsync(string imagelink)
        {
            var filestream = new FileStream(imagelink, FileMode.Open);
            var image = new Image(filestream);
            await Context.Client.CurrentUser.ModifyAsync(x => x.Avatar = image);
            EmbedBuilder eb = new EmbedBuilder
            {
                Title = "Profilkép",
                Description = "Profilkép beállítva erre:",
                ImageUrl = imagelink
            };
            await ReplyAsync(null, false, eb.Build());
        }
        [Command("setname"), Alias("name", "sn")]
        [RequireOwner]
        public async Task SetNicknameAsync(string name)
        {
            var oldName = Context.Client.CurrentUser.Username;
            await Context.Client.CurrentUser.ModifyAsync(x => x.Username = name);
            await ReplyAsync($"**Név megváltoztatva ** `{oldName}` ** -> ** `{name}`");
        }
    }
}
