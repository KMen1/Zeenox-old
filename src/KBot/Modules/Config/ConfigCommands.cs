using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Models;

namespace KBot.Modules.Config;

[RequireUserPermission(GuildPermission.Administrator)]
[Group("config", "Bot beállításai")]
public class SetupCommands : KBotModuleBase
{
    [Group("announcements", "Bejelentések beállításai")]
    public class Announcements : KBotModuleBase
    {
        [SlashCommand("enable", "Bejelentések engedélyezése/letiltása")]
        public async Task EnableAnnouncementsAsync(bool enable)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.Announcements.Enabled = enable).ConfigureAwait(false);
            await RespondAsync(enable ? "Bejelentések bekapcsolva!" : "Bejelentések kikapcsolva!", ephemeral: true).ConfigureAwait(false);
        }
        
        [SlashCommand("join", "Szerverre való csatlakozás bejelentő csatornája")]
        public async Task SetJoinAsync(ITextChannel channel)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.Announcements.JoinChannelId = channel.Id).ConfigureAwait(false);
            await RespondAsync("Csatorna beállítva!", ephemeral: true).ConfigureAwait(false);
        }
        
        [SlashCommand("autorole", "Szerverre való csatlakozás bejelentő csatornája")]
        public async Task SetAutoRoleAsync(IRole role)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.Announcements.JoinRoleId = role.Id).ConfigureAwait(false);
            await RespondAsync("Csatorna beállítva!", ephemeral: true).ConfigureAwait(false);
        }
        
        [SlashCommand("leave", "Szerverről való kilépés bejelentő csatornája")]
        public async Task SetLeaveAsync(ITextChannel channel)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.Announcements.LeftChannelId = channel.Id).ConfigureAwait(false);
            await RespondAsync("Csatorna beállítva!", ephemeral: true).ConfigureAwait(false);
        }

        [SlashCommand("ban", "Szerverről való kitiltás bejelentő csatornája")]
        public async Task SetBanAsync(ITextChannel channel)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.Announcements.BanChannelId = channel.Id).ConfigureAwait(false);
            await RespondAsync("Csatorna beállítva!", ephemeral: true).ConfigureAwait(false);
        }
        
        [SlashCommand("unban", "Szerverről való kitiltás megszüntetésének bejelentő csatornája")]
        public async Task SetUnbanAsync(ITextChannel channel)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.Announcements.UnbanChannelId = channel.Id).ConfigureAwait(false);
            await RespondAsync("Csatorna beállítva!", ephemeral: true).ConfigureAwait(false);
        }
    }

    [Group("temporaryvoice", "Ideiglenes hangcsatornák beállításai")]
    public class TemporaryVoice : KBotModuleBase
    {
        [SlashCommand("enable", "Ideiglenes hangcsatornák engedélyezése/letiltása")]
        public async Task EnableTemporaryVoiceAsync(bool enable)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.TemporaryVoice.Enabled = enable).ConfigureAwait(false);
            await RespondAsync(enable ? "Ideiglenes hangcsatornák bekapcsolva!" : "Ideiglenes hangcsatornák kikapcsolva!", ephemeral: true).ConfigureAwait(false);
        }

        [SlashCommand("category", "Ideiglenes hangcsatornák kategóriája")]
        public async Task SetCategoryAsync(ICategoryChannel channel)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.TemporaryVoice.CategoryId = channel.Id).ConfigureAwait(false);
            await RespondAsync("Kategória beállítva!", ephemeral: true).ConfigureAwait(false);
        }

        [SlashCommand("channel", "Ideiglenes hangcsatornák létrehozás csatonája")]
        public async Task SetChannelAsync(IVoiceChannel channel)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.TemporaryVoice.CreateChannelId = channel.Id).ConfigureAwait(false);
            await RespondAsync("Csatorna beállítva!", ephemeral: true).ConfigureAwait(false);
        }
    }
    
    [Group("leveling", "Szintrendszer beállításai")]
    public class Leveling : KBotModuleBase
    {
        [SlashCommand("enable", "Szintrendszer engedélyezése/letiltása")]
        public async Task EnableLevelingAsync(bool enable)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.Leveling.Enabled = enable).ConfigureAwait(false);
            await RespondAsync(enable ? "Szintrendszer bekapcsolva!" : "Szintrendszer kikapcsolva!", ephemeral: true).ConfigureAwait(false);
        }
        
        [SlashCommand("channel", "Szintlépések csatornája")]
        public async Task SetChannelAsync(ITextChannel channel)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.Leveling.AnnounceChannelId = channel.Id).ConfigureAwait(false);
            await RespondAsync("Csatorna beállítva!", ephemeral: true).ConfigureAwait(false);
        }

        [SlashCommand("afk", "AFK csatorna")]
        public async Task SetAfkChannelAsync(IVoiceChannel channel)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.Leveling.AfkChannelId = channel.Id).ConfigureAwait(false);
            await RespondAsync("Csatorna beállítva!", ephemeral: true).ConfigureAwait(false);
        }
        
        [SlashCommand("addrole", "Szintlépések esetén hozzáadandó rang hozzáadása")]
        public async Task AddRoleAsync(IRole role, [MinValue(1)]int level)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.Leveling.LevelRoles.Add(new LevelRole(role, level))).ConfigureAwait(false);
            await RespondAsync($"Rang hozzáadva! ({level} szint)", ephemeral: true).ConfigureAwait(false);
        }
        
        [SlashCommand("removerole", "Szintlépések esetén hozzáadandó rang eltávolítása")]
        public async Task RemoveRoleAsync(IRole role, [MinValue(1)]int level)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.Leveling.LevelRoles.RemoveAll(x => x.Id == role.Id)).ConfigureAwait(false);
            await RespondAsync($"Rang eltávolítva! ({level} szint)", ephemeral: true).ConfigureAwait(false);
        }
        
    }
    
    [Group("suggestions", "Javaslatok beállításai")]
    public class Suggestions : KBotModuleBase
    {
        [SlashCommand("enable", "Javaslatok engedélyezése/letiltása")]
        public async Task EnableSuggestionsAsync(bool enable)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.Suggestions.Enabled = enable).ConfigureAwait(false);
            await RespondAsync(enable ? "Javaslatok bekapcsolva!" : "Javaslatok kikapcsolva!", ephemeral: true).ConfigureAwait(false);
        }
        
        [SlashCommand("channel", "Javaslatok csatornája")]
        public async Task SetChannelAsync(ITextChannel channel)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.Suggestions.AnnounceChannelId = channel.Id).ConfigureAwait(false);
            await RespondAsync("Csatorna beállítva!", ephemeral: true).ConfigureAwait(false);
        }
    }
    
    [Group("tourevents", "Túra események beállításai")]
    public class TourEvents : KBotModuleBase
    {
        [SlashCommand("enable", "Túra események engedélyezése/letiltása")]
        public async Task EnableTourEventsAsync(bool enable)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.TourEvents.Enabled = enable).ConfigureAwait(false);
            await RespondAsync(enable ? "Túra események bekapcsolva!" : "Túra események kikapcsolva!", ephemeral: true).ConfigureAwait(false);
        }
        
        [SlashCommand("channel", "Túra események bejelentő csatornája")]
        public async Task SetChannelAsync(ITextChannel channel)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.TourEvents.AnnounceChannelId = channel.Id).ConfigureAwait(false);
            await RespondAsync("Csatorna beállítva!", ephemeral: true).ConfigureAwait(false);
        }
        
        [SlashCommand("role", "Túra rang")]
        public async Task SetRoleAsync(IRole role)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.TourEvents.RoleId = role.Id).ConfigureAwait(false);
            await RespondAsync("Rang beállítva!", ephemeral: true).ConfigureAwait(false);
        }
    }
    
    [Group("movieevents", "Film események beállításai")]
    public class MovieEvents : KBotModuleBase
    {
        [SlashCommand("enable", "Film események engedélyezése/letiltása")]
        public async Task EnableMovieEventsAsync(bool enable)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.MovieEvents.Enabled = enable).ConfigureAwait(false);
            await RespondAsync(enable ? "Film események bekapcsolva!" : "Film események kikapcsolva!", ephemeral: true).ConfigureAwait(false);
        }
        
        [SlashCommand("channel", "Film események bejelentő csatornája")]
        public async Task SetChannelAsync(ITextChannel channel)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.MovieEvents.AnnounceChannelId = channel.Id).ConfigureAwait(false);
            await RespondAsync("Csatorna beállítva!", ephemeral: true).ConfigureAwait(false);
        }
        
        [SlashCommand("role", "Film rang")]
        public async Task SetRoleAsync(IRole role)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.MovieEvents.RoleId = role.Id).ConfigureAwait(false);
            await RespondAsync("Rang beállítva!", ephemeral: true).ConfigureAwait(false);
        }
        
        [SlashCommand("streamchannel", "Film események stream csatornája")]
        public async Task SetStreamChannelAsync(ITextChannel channel)
        {
            await Database.UpdateGuildConfigAsync(Context.Guild, x => x.MovieEvents.StreamChannelId = channel.Id).ConfigureAwait(false);
            await RespondAsync("Csatorna beállítva!", ephemeral: true).ConfigureAwait(false);
        }
    }
}