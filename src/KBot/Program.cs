using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using CloudinaryDotNet;
using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using KBot.Models;
using KBot.Services;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using OsuSharp;
using OsuSharp.Extensions;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using ILogger = Lavalink4NET.Logging.ILogger;

namespace KBot;

public static class Program
{
    private static Task Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
            .WriteTo.Console()
            .WriteTo.Sentry(x =>
            {
                x.MinimumBreadcrumbLevel = LogEventLevel.Warning;
                x.MinimumEventLevel = LogEventLevel.Warning;
                x.Dsn = "";
                x.Debug = false;
                x.AttachStacktrace = true;
                x.SendDefaultPii = true;
                x.TracesSampleRate = 1.0;
                x.Release = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
#if DEBUG
                x.Environment = "debug";
#else
                x.Environment = "production";
#endif
            })
            .CreateLogger();

        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(x =>
            {
                x.AddConfiguration(new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("config.json")
                    .Build());
            })
            .ConfigureDiscordHost((context, config) =>
            {
                config.SocketConfig = new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Verbose,
                    AlwaysDownloadUsers = true,
                    MessageCacheSize = 100,
                    GatewayIntents = GatewayIntents.All,
                    LogGatewayIntentWarnings = false
                };
                config.Token = context.Configuration.GetSection("Client").GetValue<string>("Token");
            })
            .UseInteractionService((_, config) =>
            {
                config.DefaultRunMode = RunMode.Async;
                config.LogLevel = LogSeverity.Verbose;
                config.UseCompiledLambda = true;
            })
            .ConfigureOsuSharp((context, options) =>
            {
                options.Configuration = new OsuClientConfiguration
                {
                    ClientId = context.Configuration.GetSection("OsuApi").GetValue<long>("AppId"),
                    ClientSecret = context.Configuration.GetSection("OsuApi").GetValue<string>("AppSecret")
                };
            })
            .ConfigureServices((context, services) =>
            {
                var config = context.Configuration.Get<BotConfig>();
                services.AddSingleton(config);
                services.AddHostedService<InteractionHandler>();
                services.AddSingleton<IDiscordClientWrapper, DiscordClientWrapper>();
                services.AddSingleton<IAudioService, LavalinkNode>();
                services.AddSingleton<LavalinkNode>();
                services.AddSingleton(new LavalinkNodeOptions
                {
                    AllowResuming = true,
                    Password = config.Lavalink.Password,
                    WebSocketUri = $"ws://{config.Lavalink.Host}:{config.Lavalink.Port}",
                    RestUri = $"http://{config.Lavalink.Host}:{config.Lavalink.Port}",
                    DisconnectOnStop = false
                });
                services.AddSingleton<ILogger, EventLogger>();
                services.AddSingleton<IMongoClient>(new MongoClient(config.MongoDb.ConnectionString));
                services.AddSingleton(x => x.GetService<IMongoClient>()!.GetDatabase(config.MongoDb.Database));
                services.AddSingleton<OsuClient>();
                services.Scan(scan => scan.FromAssemblyOf<IInjectable>()
                    .AddClasses(x => x.AssignableTo(typeof(IInjectable)))
                    .AsSelfWithInterfaces()
                    .WithSingletonLifetime());
                services.AddSingleton(new Cloudinary(new Account(
                    config.Cloudinary.CloudName,
                    config.Cloudinary.ApiKey,
                    config.Cloudinary.ApiSecret)));
                services.AddMemoryCache();
                services.AddSingleton<InteractiveService>();
                services.AddHttpClient();
                services.AddSingleton(new BaseClientService.Initializer
                {
                    ApiKey = config.Google.ApiKey,
                    ApplicationName = "KBot"
                });
                services.AddSingleton<YouTubeService>();
                services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(new ConfigurationOptions()
                {
                    EndPoints = { "redis-15978.c135.eu-central-1-1.ec2.cloud.redislabs.com:15978" },
                    Password = "5k0wjJKEdYriKtSyo5ZG9F6ohg0VBADT"
                }));
            })
            .UseSerilog()
            .UseConsoleLifetime()
            .Build();
#if RELEASE
        var link = (IShellLink) new ShellLink();
        link.SetDescription("KBot");
        link.SetPath($"C:\\KBot\\{FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}\\KBot.exe");
        var file = (IPersistFile) link;
        const string startupPath = @"C:\Users\user\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup";
        file.Save(Path.Combine(startupPath, "KBot.lnk"), false);
#endif
        foreach (var mytype in Assembly.GetExecutingAssembly().GetTypes()
                     .Where(mytype => mytype.GetInterfaces().Contains(typeof(IInjectable))))
            host.Services.GetService(mytype);
        return host.RunAsync();
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLink
    {
        void GetPath([Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd,
            int fFlags);

        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);

        void GetIconLocation([Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath,
            out int piIcon);

        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
}