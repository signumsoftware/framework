using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Selenium;

public static class Logger
{
    private static ILogger? logger;

    private static void Initialize()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        logger = loggerFactory.CreateLogger("Signum.Selenium");
    }

    public static void Log(string message)
    {
        if (logger == null)
            Initialize();

        logger?.LogInformation($"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}] {message}");
    }
}
