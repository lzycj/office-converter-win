using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.Async;

namespace OfficeConverter.Infrastructure.Logging;

public static class LoggingBootstrapper
{
    public static (ILoggerFactory factory, string logsDir) Initialize(string? baseDir = null)
    {
        baseDir ??= Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logsDir = Path.Combine(baseDir, "OfficeConverter", "logs");
        Directory.CreateDirectory(logsDir);

        var logFile = Path.Combine(logsDir, "app-.log");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Async(a => a.File(logFile, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10, restrictedToMinimumLevel: LogEventLevel.Information))
            .CreateLogger();

        var factory = new SerilogLoggerFactory(Log.Logger, dispose: true);
        return (factory, logsDir);
    }
}
