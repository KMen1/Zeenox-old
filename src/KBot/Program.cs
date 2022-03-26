using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CloudinaryDotNet;
using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using KBot.Models;
using KBot.Modules.DeadByDaylight;
using KBot.Modules.Events;
using KBot.Modules.Gambling;
using KBot.Modules.Gambling.Objects;
using KBot.Modules.Leveling;
using KBot.Modules.Music;
using KBot.Services;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using OsuSharp;
using OsuSharp.Extensions;
using Serilog;
using Serilog.Events;
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
                x.Dsn = "https://fdd00dc16d0047139121570b692abcb4@o88188.ingest.sentry.io/6201115";
                x.Debug = false;
                x.AttachStacktrace = true;
                x.SendDefaultPii = true;
                x.TracesSampleRate = 1.0;
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
                config.SocketConfig = new DiscordSocketConfig()
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
                options.Configuration = new OsuClientConfiguration()
                {
                    ClientId = context.Configuration.GetSection("OsuApi").GetValue<long>("AppId"),
                    ClientSecret = context.Configuration.GetSection("OsuApi").GetValue<string>("AppSecret"),
                };
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton(context.Configuration.Get<BotConfig>());
                services.AddSingleton(new InteractiveConfig
                {
                    DefaultTimeout = new TimeSpan(0, 0, 5, 0),
                    LogLevel = LogSeverity.Verbose
                });
                services.AddSingleton<InteractiveService>();
                services.AddHostedService<InteractionHandler>();
                services.AddSingleton<IDiscordClientWrapper, DiscordClientWrapper>();
                services.AddSingleton<IAudioService, LavalinkNode>();
                services.AddSingleton<LavalinkNode>();
                services.AddSingleton(new LavalinkNodeOptions
                {
                    AllowResuming = true,
                    Password = "youshallnotpass",
                    WebSocketUri = "ws://127.0.0.1:2333",
                    RestUri = "http://127.0.0.1:2333",
                    DisconnectOnStop = false,
                });
                services.AddSingleton<ILogger, EventLogger>();
                services.AddSingleton<AudioService>();
                services.AddSingleton<IMongoClient>(new MongoClient(context.Configuration.GetSection("MongoDb").GetValue<string>("ConnectionString")));
                services.AddSingleton(x => x.GetService<IMongoClient>()!.GetDatabase(context.Configuration.Get<BotConfig>().MongoDb.Database));
                services.AddSingleton<DatabaseService>();
                services.AddSingleton<OsuClient>();
                services.AddSingleton<LoggingService>();
                services.AddSingleton<GuildEvents>();
                services.AddSingleton<LevelingModule>();
                services.AddSingleton<DbDService>();
                services.AddSingleton<GamblingService>();
                services.AddSingleton(new Cloudinary(new Account(
                    context.Configuration.GetSection("Cloudinary").GetValue<string>("CloudName"),
                    context.Configuration.GetSection("Cloudinary").GetValue<string>("ApiKey"),
                    context.Configuration.GetSection("Cloudinary").GetValue<string>("ApiSecret"))));
                services.AddMemoryCache();
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
        return host.RunAsync();
        //[(E*100 - H)/(E-H)]/100
        /*double e = Math.Pow(2, 256);
        var c = 0;

        for (int i = 0; i < 10000; i++)
        {
            
            double h = Generator.NextDouble(0, e - 1);
            var f = 0.80 * e / (e-h);
            if (f < 1.10)
                c++;
            Console.WriteLine(f);
        }
        Console.WriteLine(c);
        return Task.CompletedTask;*/
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink { }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLink
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
}