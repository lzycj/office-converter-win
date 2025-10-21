using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OfficeConverter.Core;
using OfficeConverter.Core.Models;
using OfficeConverter.Core.Runtime;

namespace OfficeConverter.Converters.OfficeInterop;

public class InteropConverter : IConverter
{
    private static readonly string[] Inputs = new[] { ".doc", ".docx", ".ppt", ".pptx", ".rtf" };

    public IEnumerable<string> SupportedInputs => Inputs;

    public int Probe(string inputPath)
    {
        var ext = Path.GetExtension(inputPath).ToLowerInvariant();
        return Inputs.Contains(ext) ? 100 : 0;
    }

    public async Task<ConversionResult> ConvertAsync(ConversionJob job, CancellationToken ct)
    {
        // Placeholder: simulate work and create output file
        var outputs = job.Options.TryGetValue(JobOptionKeys.OutputPaths, out var list) && list is IEnumerable<string> arr
            ? arr.ToList()
            : new List<string> { Path.ChangeExtension(job.InputPath, job.TargetFormat) };

        await Task.Delay(200, ct);
        foreach (var o in outputs)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(o)!);
            await File.WriteAllTextAsync(o, $"Converted by Interop at {DateTime.Now}", ct);
        }
        return new ConversionResult
        {
            Success = true,
            OutputPaths = outputs
        };
    }
}
