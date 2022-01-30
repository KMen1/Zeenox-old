using System;
using KBot.Config;
using Microsoft.Extensions.Configuration;

namespace KBot;

public static class Program
{
    private static void Main()
    {
        var root = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("config.json").Build();
        new Bot().StartAsync(root.Get<BotConfig>()).GetAwaiter().GetResult();
    }
}