namespace KBot.Models;

public class BotConfig
{
    public BotConfig(
        ClientConfig client,
        LavalinkConfig lavalink,
        MongoDbConfig mongoDb,
        OsuApiConfig osuApi,
        CloudinaryConfig cloudinary,
        GoogleConfig google,
        RedisConfig redis)
    {
        Client = client;
        Lavalink = lavalink;
        MongoDb = mongoDb;
        OsuApi = osuApi;
        Cloudinary = cloudinary;
        Google = google;
        Redis = redis;
    }

    public ClientConfig Client { get; init; }
    public LavalinkConfig Lavalink { get; init; }
    public MongoDbConfig MongoDb { get; init; }
    public OsuApiConfig OsuApi { get; init; }
    public CloudinaryConfig Cloudinary { get; init; }
    public GoogleConfig Google { get; init; }
    public RedisConfig Redis { get; init; }
}

public class ClientConfig
{
    public ClientConfig(string token, string game)
    {
        Token = token;
        Game = game;
    }

    public string Token { get; init; }
    public string Game { get; init; }
}

public class LavalinkConfig
{
    public LavalinkConfig(string host, ushort port, string password)
    {
        Host = host;
        Port = port;
        Password = password;
    }

    public string Host { get; init; }
    public ushort Port { get; init; }
    public string Password { get; init; }
}

public class MongoDbConfig
{
    public MongoDbConfig(
        string connectionString,
        string database,
        string guildCollection,
        string configCollection,
        string userCollection,
        string transactionCollection,
        string warnCollection,
        string buttonRoleCollection)
    {
        ConnectionString = connectionString;
        Database = database;
        GuildCollection = guildCollection;
        ConfigCollection = configCollection;
        UserCollection = userCollection;
        TransactionCollection = transactionCollection;
        WarnCollection = warnCollection;
        ButtonRoleCollection = buttonRoleCollection;
    }

    public string ConnectionString { get; init; }
    public string Database { get; init; }
    public string GuildCollection { get; init; }
    public string ConfigCollection { get; init; }
    public string UserCollection { get; init; }
    public string TransactionCollection { get; init; }
    public string WarnCollection { get; init; }
    public string ButtonRoleCollection { get; init; }
}

public class OsuApiConfig
{
    public OsuApiConfig(ulong appId, string appSecret)
    {
        AppId = appId;
        AppSecret = appSecret;
    }

    public ulong AppId { get; init; }
    public string AppSecret { get; init; }
}

public class CloudinaryConfig
{
    public CloudinaryConfig(string cloudName, string apiKey, string apiSecret)
    {
        CloudName = cloudName;
        ApiKey = apiKey;
        ApiSecret = apiSecret;
    }

    public string CloudName { get; init; }
    public string ApiKey { get; init; }
    public string ApiSecret { get; init; }
}

public class GoogleConfig
{
    public GoogleConfig(string apiKey)
    {
        ApiKey = apiKey;
    }

    public string ApiKey { get; init; }
}

public class RedisConfig
{
    public RedisConfig(string endpoint)
    {
        Endpoint = endpoint;
    }

    public string Endpoint { get; init; }
}