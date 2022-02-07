namespace KBot.Config;

public class BotConfig
{
    public ClientConfig Client { get; init; }

    public LavalinkConfig Lavalink { get; init; }

    public MongoDbConfig MongoDb { get; init; }

    public OsuApiConfig OsuApi { get; init; }
}

public class ClientConfig
{
    public string Token { get; init; }

    public string Game { get; init; }
}

public class LavalinkConfig
{
    public string Host { get; init; }

    public ushort Port { get; init; }

    public string Password { get; init; }
}

public class MongoDbConfig
{
    public string ConnectionString { get; init; }

    public string Database { get; init; }

    public string Collection { get; init; }
}

public class OsuApiConfig
{
    public ulong AppId { get; init; }

    public string AppSecret { get; init; }
}