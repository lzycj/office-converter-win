using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace OfficeConverter.Infrastructure.Config;

public static class ConfigLoader
{
    public static AppConfig Load(string? basePath = null)
    {
        basePath ??= AppContext.BaseDirectory;

        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        // Also consider a user-level config under AppData/OfficeConverter
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var userConfigDir = Path.Combine(appData, "OfficeConverter");
        var userConfigPath = Path.Combine(userConfigDir, "appsettings.json");
        if (File.Exists(userConfigPath))
        {
            builder.AddJsonFile(userConfigPath, optional: true, reloadOnChange: true);
        }

        var config = builder.Build();
        var cfg = new AppConfig();
        config.Bind(cfg);
        if (!string.IsNullOrWhiteSpace(cfg.OutputDir))
        {
            cfg.OutputDir = Environment.ExpandEnvironmentVariables(cfg.OutputDir);
        }
        Directory.CreateDirectory(cfg.OutputDir);
        return cfg;
    }
}
