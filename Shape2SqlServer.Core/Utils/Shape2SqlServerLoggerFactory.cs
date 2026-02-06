#nullable enable
using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace Shape2SqlServer.Core;

/// <summary>
/// Provides centralized logger access for Shape2SqlServer components.
/// Replaces the legacy TraceSource-based logging system.
/// </summary>
public static class Shape2SqlServerLoggerFactory
{
    private static ILoggerFactory? _loggerFactory;
    private static Microsoft.Extensions.Logging.ILogger? _logger;

    /// <summary>
    /// Gets or sets the logger factory used to create loggers.
    /// If not set, a default logger factory will be used with console, debug, and file logging.
    /// </summary>
    public static ILoggerFactory LoggerFactory
    {
        get
        {
            if (_loggerFactory == null)
            {
                // Configure Serilog for file logging
                var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "shape2sqlserver-.log");

                var serilogLogger = new LoggerConfiguration()
                    .MinimumLevel.Warning()
                    .WriteTo.File(
                        path: logFilePath,
                        rollingInterval: RollingInterval.Day,
                        fileSizeLimitBytes: 10_485_760, // 10 MB
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 7)
                    .CreateLogger();

                _loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                {
                    builder
                        .AddConsole()
                        .AddDebug()
                        .AddSerilog(serilogLogger)
                        .SetMinimumLevel(LogLevel.Warning);
                });
            }
            return _loggerFactory;
        }
        set => _loggerFactory = value;
    }

    /// <summary>
    /// Gets the default logger instance for Shape2SqlServer.
    /// </summary>
    public static Microsoft.Extensions.Logging.ILogger Logger
    {
        get
        {
            if (_logger == null)
            {
                _logger = LoggerFactory.CreateLogger("Shape2SqlServer");
            }
            return _logger;
        }
    }

    /// <summary>
    /// Creates a logger for a specific category.
    /// </summary>
    public static Microsoft.Extensions.Logging.ILogger<T> CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
}
