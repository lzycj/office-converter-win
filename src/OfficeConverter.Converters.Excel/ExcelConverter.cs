using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OfficeConverter.Core;
using OfficeConverter.Core.Models;
using OfficeConverter.Core.Runtime;

namespace OfficeConverter.Converters.Excel;

public class ExcelConverter : IConverter
{
    private static readonly string[] Inputs = new[] { ".xls", ".xlsx", ".csv" };

    public IEnumerable<string> SupportedInputs => Inputs;

    public int Probe(string inputPath)
    {
        var ext = Path.GetExtension(inputPath).ToLowerInvariant();
        return Inputs.Contains(ext) ? 100 : 0;
    }

    public async Task<ConversionResult> ConvertAsync(ConversionJob job, CancellationToken ct)
    {
        var outputs = job.Options.TryGetValue(JobOptionKeys.OutputPaths, out var list) && list is IEnumerable<string> arr
            ? arr.ToList()
            : new List<string> { Path.ChangeExtension(job.InputPath, job.TargetFormat) };

        // Simulate failure when filename contains 'fail'
        if (Path.GetFileName(job.InputPath).Contains("fail", StringComparison.OrdinalIgnoreCase))
        {
            await Task.Delay(100, ct);
            return new ConversionResult
            {
                Success = false,
                ErrorCode = "SIM_FAIL",
                ErrorMessage = "Simulated failure by filename"
            };
        }

        await Task.Delay(150, ct);
        foreach (var o in outputs)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(o)!);
            await File.WriteAllTextAsync(o, $"Converted by Excel at {DateTime.Now}", ct);
        }
        return new ConversionResult
        {
            Success = true,
            OutputPaths = outputs
        };
    }
}
