using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OfficeConverter.Core;
using OfficeConverter.Core.Models;
using OfficeConverter.Core.Runtime;

namespace OfficeConverter.Converters.Pandoc;

public class PandocConverter : IConverter
{
    private static readonly string[] Inputs = new[] { ".md", ".markdown", ".txt", ".html" };

    public IEnumerable<string> SupportedInputs => Inputs;

    public int Probe(string inputPath)
    {
        var ext = Path.GetExtension(inputPath).ToLowerInvariant();
        return Inputs.Contains(ext) ? 80 : 0;
    }

    public async Task<ConversionResult> ConvertAsync(ConversionJob job, CancellationToken ct)
    {
        var outputs = job.Options.TryGetValue(JobOptionKeys.OutputPaths, out var list) && list is IEnumerable<string> arr
            ? arr.ToList()
            : new List<string> { Path.ChangeExtension(job.InputPath, job.TargetFormat) };

        await Task.Delay(120, ct);
        foreach (var o in outputs)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(o)!);
            await File.WriteAllTextAsync(o, $"Converted by Pandoc at {DateTime.Now}", ct);
        }
        return new ConversionResult
        {
            Success = true,
            OutputPaths = outputs
        };
    }
}
