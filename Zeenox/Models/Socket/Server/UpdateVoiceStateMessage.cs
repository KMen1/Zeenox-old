using Discord.WebSocket;

namespace Zeenox.Models.Socket.Server;

public readonly struct UpdateVoiceStateMessage : IServerMessage
{
    public bool IsBotConnected { get; init; }
    public bool IsUserConnected { get; init; }
    public bool IsInSameChannel { get; init; }
    public string? BotChannelName { get; init; }
    public string? UserChannelName { get; init; }

    public static UpdateVoiceStateMessage GetVoiceStateMessage(DiscordShardedClient client, ulong guildId, ulong userId)
    {
        var guild = client.GetGuild(guildId);
        var user = guild.GetUser(userId);
        var bot = guild.GetUser(client.CurrentUser.Id);

        var botChannel = bot.VoiceChannel;
        var userChannel = user.VoiceChannel;

        return new UpdateVoiceStateMessage
        {
            IsBotConnected = botChannel != null,
            IsUserConnected = userChannel != null,
            IsInSameChannel = botChannel?.Id == userChannel?.Id,
            BotChannelName = botChannel?.Name,
            UserChannelName = userChannel?.Name
        };
    }
}