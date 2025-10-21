using System;
using System.Collections.Generic;
using System.IO;
using OfficeConverter.Core.Runtime;
using OfficeConverter.Infrastructure.Config;

namespace OfficeConverter.Infrastructure.Output;

public class DefaultOutputPathResolver : IOutputPathResolver
{
    private readonly AppConfig _config;

    public DefaultOutputPathResolver(AppConfig config)
    {
        _config = config;
    }

    public IEnumerable<string> GetOutputPaths(string inputPath, string targetFormat)
    {
        var name = Path.GetFileNameWithoutExtension(inputPath);
        var ext = targetFormat.TrimStart('.');
        var fileName = _config.NamingTemplate
            .Replace("{name}", name, StringComparison.OrdinalIgnoreCase)
            .Replace("{ext}", ext, StringComparison.OrdinalIgnoreCase);

        yield return Path.Combine(_config.OutputDir, fileName);
    }
}
