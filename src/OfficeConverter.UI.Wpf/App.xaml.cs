using System;
using System.Windows;
using Microsoft.Extensions.Logging;
using OfficeConverter.Converters.Excel;
using OfficeConverter.Converters.OfficeInterop;
using OfficeConverter.Converters.Pandoc;
using OfficeConverter.Core;
using OfficeConverter.Core.Runtime;
using OfficeConverter.Infrastructure.Config;
using OfficeConverter.Infrastructure.Hashing;
using OfficeConverter.Infrastructure.Logging;
using OfficeConverter.Infrastructure.Output;

namespace OfficeConverter.UI.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Logging
        var (loggerFactory, logsDir) = LoggingBootstrapper.Initialize();
        var logger = loggerFactory.CreateLogger("App");

        // Configuration
        var config = ConfigLoader.Load(AppContext.BaseDirectory);

        // Runtime services
        var hasher = new Sha256Hasher();
        var outputResolver = new DefaultOutputPathResolver(config);
        var jobLoggerFactory = new JobLoggerFactory(logsDir);
        var options = new DispatcherOptions
        {
            Concurrency = Math.Max(1, config.Concurrency),
            MaxAttempts = Math.Max(1, config.MaxAttempts)
        };

        var converters = new IConverter[]
        {
            new InteropConverter(),
            new ExcelConverter(),
            new PandocConverter()
        };

        Services.Dispatcher = new ConversionDispatcher(converters, logger, options, hasher, outputResolver, jobLoggerFactory);
        Services.LoggerFactory = loggerFactory;
        Services.Config = config;
    }
}

public static class Services
{
    public static IConversionDispatcher Dispatcher { get; set; } = null!;
    public static ILoggerFactory LoggerFactory { get; set; } = null!;
    public static AppConfig Config { get; set; } = null!;
}
