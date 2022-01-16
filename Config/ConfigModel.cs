namespace KBot.Config;

public abstract class ConfigModel
{
    public class Config
    {
        public ClientConfig Client { get; set; }
        
        public LavalinkConfig Lavalink { get; set; }
        
        public MongoDbConfig MongoDb { get; set; }
        
        public AnnouncementConfig Announcements { get; set; }
        
        public TemporaryVoiceChannelConfig TemporaryVoiceChannels { get; set; }
        
        public MovieConfig Movie { get; set; }
        
        public TourConfig Tour { get; set; }
        
        public LevelingConfig Leveling { get; set; }
    }

    public class ClientConfig
    {
        public string Token { get; set; }
        
        public string Game { get; set; }
    }
    
    public class LavalinkConfig
    {
        public bool Enabled { get; set; }
        
        public string Host { get; set; }
        
        public ushort Port { get; set; }
        
        public string Password { get; set; }
    }
    
    public class MongoDbConfig
    {
        public string ConnectionString { get; set; }
        
        public string Database { get; set; }
        
        public string Collection { get; set; }
    }
    public class AnnouncementConfig
    {
        public bool Enabled { get; set; }
        
        public ulong UserAnnouncementChannelId { get; set; }
    }
    
    public class TemporaryVoiceChannelConfig
    {
        public bool Enabled { get; set; }
        
        public ulong CategoryId { get; set; }
        
        public ulong CreateChannelId { get; set; }
    }

    public class MovieConfig
    {
        public bool Enabled { get; set; }
        
        public ulong EventAnnouncementChannelId { get; set; }
        
        public ulong StreamingChannelId { get; set; }
        
        public ulong RoleId { get; set; }
    }
    
    public class TourConfig
    {
        public bool Enabled { get; set; }
        
        public ulong EventAnnouncementChannelId { get; set; }
        
        public ulong RoleId { get; set; }
    }
    
    public class LevelingConfig
    {
        public bool Enabled { get; set; }
        
        public int PointsToLevelUp { get; set; }
    }
}