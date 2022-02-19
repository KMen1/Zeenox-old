﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using KBot.Models;
using KBot.Modules.Announcements;
using KBot.Modules.Audio;
using KBot.Modules.DeadByDaylight;
using KBot.Modules.Leveling;
using KBot.Modules.OSU;
using KBot.Modules.TemporaryChannels;
using KBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32.TaskScheduler;
using MongoDB.Driver;
using OsuSharp;
using OsuSharp.Extensions;
using Sentry;
using Serilog;
using Serilog.Events;
using Victoria;
using Task = System.Threading.Tasks.Task;

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
                services.AddSingleton(new LavaConfig
                {
                    Hostname = context.Configuration.GetSection("LavaLink").GetValue<string>("Host"),
                    Port = context.Configuration.GetSection("LavaLink").GetValue<ushort>("Port"),
                    Authorization = context.Configuration.GetSection("LavaLink").GetValue<string>("Password"),
                    LogSeverity = LogSeverity.Verbose
                });
                services.AddSingleton<LavaNode>();
                services.AddSingleton(new InteractiveConfig()
                {
                    DefaultTimeout = new TimeSpan(0, 0, 5, 0),
                    LogLevel = LogSeverity.Verbose
                });
                services.AddSingleton<InteractiveService>();
                services.AddHostedService<InteractionHandler>();
                services.AddSingleton<AudioService>();
                services.AddSingleton<IHostedService, AudioService>(x => x.GetService<AudioService>());
                services.AddSingleton<IMongoClient>(new MongoClient(context.Configuration.GetSection("MongoDb").GetValue<string>("ConnectionString")));
                services.AddSingleton(x => x.GetService<IMongoClient>()!.GetDatabase(context.Configuration.Get<BotConfig>().MongoDb.Database));
                services.AddSingleton<DatabaseService>();
                services.AddSingleton<OsuClient>();
                services.AddHostedService<LoggingService>();
                services.AddHostedService<AnnouncementsModule>();
                services.AddHostedService<MovieModule>();
                services.AddHostedService<TourModule>();
                services.AddHostedService<TemporaryVoiceModule>();
                services.AddHostedService<LevelingModule>();
                services.AddSingleton<DbDService>();
                services.AddMemoryCache();
            })
            .UseSerilog()
            .UseConsoleLifetime()
            .Build();

        /*var wt = new WeeklyTrigger();
        wt.StartBoundary = DateTime.Today.AddHours(17).AddMinutes(15);
        wt.DaysOfWeek = DaysOfTheWeek.Thursday;
        wt.WeeksInterval = 1;
        var td = TaskService.Instance.NewTask();
        td.RegistrationInfo.Description = "KBot - Epic Free Games";
        td.Triggers.Add(wt);
        td.Settings.Compatibility = TaskCompatibility.V2_3;
        td.Actions.Add($"C:\\KBot\\{FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}\\Epic\\KBotEpic.exe");
        TaskService.Instance.RootFolder.RegisterTaskDefinition("KBot - Epic Free Games", td);*/

        IShellLink link = (IShellLink)new ShellLink();
        link.SetDescription("KBot");
        link.SetPath($"C:\\KBot\\{FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}\\KBot.exe");
        IPersistFile file = (IPersistFile)link;
        // C:\Users\Oli\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup
        // C:\Users\user\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup
        const string startupPath = @"C:\Users\user\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup";
        file.Save(Path.Combine(startupPath, "KBot.lnk"), false);

        return host.RunAsync();
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLink { }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal interface IShellLink
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