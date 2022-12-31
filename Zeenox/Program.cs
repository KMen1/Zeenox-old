using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using CloudinaryDotNet;
using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Logging;
using Lavalink4NET.Tracking;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using SpotifyAPI.Web;
using Zeenox;
using Zeenox.Services;
using ILogger = Lavalink4NET.Logging.ILogger;

Env.Load();
Log.Logger = new LoggerConfiguration().Enrich
    .FromLogContext()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
    .MinimumLevel.Override("Hangfire", LogEventLevel.Information)
    .WriteTo.Console()
    .WriteTo.Sentry(
        x =>
        {
            x.MinimumBreadcrumbLevel = LogEventLevel.Warning;
            x.MinimumEventLevel = LogEventLevel.Warning;
            x.Dsn = Environment.GetEnvironmentVariable("SENTRY_DSN");
            x.Debug = false;
            x.AttachStacktrace = true;
            x.TracesSampleRate = 1.0;
            x.Release = FileVersionInfo
                .GetVersionInfo(Assembly.GetExecutingAssembly().Location)
                .FileVersion;
        }
    )
    .CreateLogger();

var host = Host.CreateDefaultBuilder()
    .UseSerilog()
    .ConfigureWebHostDefaults(x => x.UseStartup<Startup>())
    .ConfigureDiscordShardedHost(
        (_, config) =>
        {
            config.SocketConfig = new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 200,
                GatewayIntents = GatewayIntents.All,
                LogGatewayIntentWarnings = false,
                DefaultRetryMode = RetryMode.AlwaysFail
            };
            config.Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN") ??
                           throw new ArgumentException("No token provided");
        }
    )
    .UseInteractionService(
        (_, config) =>
        {
            config.DefaultRunMode = RunMode.Async;
            config.LogLevel = LogSeverity.Verbose;
            config.UseCompiledLambda = true;
            config.LocalizationManager = new JsonLocalizationManager("Resources", "DCLocalization");
        }
    )
    .ConfigureServices(
        (_, services) =>
        {
            services.AddHostedService<InteractionHandler>();
            services.AddSingleton<IDiscordClientWrapper, DiscordClientWrapper>();
            services.AddSingleton<IAudioService, LavalinkNode>();
            services.AddSingleton(
                new LavalinkNodeOptions
                {
                    AllowResuming = true,
                    DisconnectOnStop = false,
                    WebSocketUri =
                        $"ws://{Environment.GetEnvironmentVariable("LAVALINK_HOST")}:{Environment.GetEnvironmentVariable("LAVALINK_PORT")}",
                    RestUri =
                        $"http://{Environment.GetEnvironmentVariable("LAVALINK_HOST")}:{Environment.GetEnvironmentVariable("LAVALINK_PORT")}",
                    Password = Environment.GetEnvironmentVariable("LAVALINK_PASSWORD")!
                }
            );
            services.AddSingleton(
                new InactivityTrackingOptions
                {
                    DisconnectDelay = TimeSpan.FromMinutes(3),
                    PollInterval = TimeSpan.FromSeconds(5)
                }
            );
            services.AddSingleton<InactivityTrackingService>();
            services.AddSingleton<IMongoClient>(
                new MongoClient(Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING"))
            );
            services.AddSingleton(
                new Cloudinary(
                    new Account(
                        Environment.GetEnvironmentVariable("CLOUDINARY_NAME"),
                        Environment.GetEnvironmentVariable("CLOUDINARY_KEY"),
                        Environment.GetEnvironmentVariable("CLOUDINARY_SECRET")
                    )
                )
            );
            services.AddSingleton(
                new BaseClientService.Initializer
                {
                    ApiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY"),
                    ApplicationName = "Zeenox"
                }
            );
            services.AddSingleton<YouTubeService>();
            services.AddSingleton<SearchService>();
            services.AddSingleton<AudioService>();
            services.AddSingleton<MongoService>();
            services.AddSingleton<TemporaryChannelService>();
            services.AddSingleton<GameService>();
            services.AddSingleton<SocketService>();
            services.Configure<HostOptions>(
                x =>
                    x.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore
            );
            //services.AddSingleton<ImageService>();
            services.AddHangfire(
                x =>
                {
                    x.UseMongoStorage(
                        Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING"),
                        Environment.GetEnvironmentVariable("MONGO_DATABASE"),
                        new MongoStorageOptions
                        {
                            MigrationOptions = new MongoMigrationOptions
                            {
                                MigrationStrategy = new MigrateMongoMigrationStrategy(),
                                BackupStrategy = new CollectionMongoBackupStrategy()
                            },
                            CheckConnection = true
                        }
                    );
                    x.UseSerilogLogProvider();
                }
            );
            services.AddSingleton<ILogger, EventLogger>();
            services.AddSingleton(
                SpotifyClientConfig
                    .CreateDefault()
                    .WithAuthenticator(
                        new ClientCredentialsAuthenticator(
                            Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID")!,
                            Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET")!
                        )
                    )
            );
            services.AddSingleton<SpotifyClient>();
            services.AddHangfireServer();
            services.AddHttpClient();
            services.AddMemoryCache();
        }
    )
    .UseConsoleLifetime()
    .Build();

var cache = host.Services.GetRequiredService<IMemoryCache>();

var localizationData = File.ReadAllText("Resources/Localization.json") ??
                       throw new FileNotFoundException("Localization.json not found");

var localization = JsonConvert.DeserializeObject<
                       Dictionary<string, Dictionary<string, string>>>(localizationData) ??
                   throw new JsonException("Localization.json is not valid JSON");

foreach (var item in localization) cache.Set(item.Key, item.Value);

var client = host.Services.GetRequiredService<DiscordShardedClient>();

var audioService = host.Services.GetRequiredService<IAudioService>();
var needToConnect = true;
client.ShardReady += async _ =>
{
    if (needToConnect)
    {
        await audioService.InitializeAsync().ConfigureAwait(false);
        needToConnect = false;
    }
};

client.ShardReady += async shard =>
{
    var mongo = host.Services.GetRequiredService<MongoService>();
    foreach (var guild in shard.Guilds) await mongo.AddGuildConfigAsync(guild.Id).ConfigureAwait(false);
};

var audioLogger = (EventLogger) ((LavalinkNode) audioService).Logger!;

audioLogger.LogMessage += (_, eventArgs) =>
{
    switch (eventArgs.Level)
    {
        case LogLevel.Information:
            Log.Logger.Information(eventArgs.Message);
            break;
        case LogLevel.Error:
            Log.Logger.Error(eventArgs.Exception, eventArgs.Message);
            break;
        case LogLevel.Warning:
            Log.Logger.Warning(eventArgs.Message);
            break;
        case LogLevel.Debug:
            Log.Logger.Debug(eventArgs.Message);
            break;
        case LogLevel.Trace:
            Log.Logger.Debug(eventArgs.Message);
            break;
    }
};

await host.RunAsync().ConfigureAwait(false);