using System;
using System.IO;
using Microsoft.Extensions.Logging;
using OfficeConverter.Core.Runtime;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace OfficeConverter.Infrastructure.Logging;

public class JobLoggerFactory : IJobLoggerFactory
{
    private readonly string _logsDir;

    public JobLoggerFactory(string logsDir)
    {
        _logsDir = logsDir;
    }

    public (ILogger Logger, string LogPath) Create(Guid jobId)
    {
        var path = Path.Combine(_logsDir, $"job-{jobId}.log");
        Logger serilog = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(path, rollingInterval: RollingInterval.Infinite, restrictedToMinimumLevel: LogEventLevel.Debug)
            .CreateLogger();
        var logger = new SerilogLoggerFactory(serilog, dispose: true).CreateLogger($"Job:{jobId}");
        return (logger, path);
    }
}
