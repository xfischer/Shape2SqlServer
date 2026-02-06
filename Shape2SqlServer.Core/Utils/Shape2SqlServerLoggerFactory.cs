#nullable enable
using Microsoft.Extensions.Logging;

namespace Shape2SqlServer.Core;

/// <summary>
/// Provides centralized logger access for Shape2SqlServer components.
/// Replaces the legacy TraceSource-based logging system.
/// </summary>
public static class Shape2SqlServerLoggerFactory
{
    private static ILoggerFactory? _loggerFactory;
    private static ILogger? _logger;

    /// <summary>
    /// Gets or sets the logger factory used to create loggers.
    /// If not set, a default console logger factory will be used.
    /// </summary>
    public static ILoggerFactory LoggerFactory
    {
        get
        {
            if (_loggerFactory == null)
            {
                _loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                {
                    builder
                        .AddConsole()
                        .AddDebug()
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
    public static ILogger Logger
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
    public static ILogger<T> CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
}
