using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OfficeConverter.Core;
using OfficeConverter.Core.Models;
using OfficeConverter.Core.Runtime;

namespace OfficeConverter.Tests.Fakes;

public class FakeConverter : IConverter
{
    private readonly int _failuresBeforeSuccess;
    private int _calls;
    private readonly string[] _inputs;

    public int Calls => _calls;

    public FakeConverter(int failuresBeforeSuccess, params string[] inputs)
    {
        _failuresBeforeSuccess = failuresBeforeSuccess;
        _inputs = inputs.Length == 0 ? new[] { ".txt" } : inputs;
    }

    public IEnumerable<string> SupportedInputs => _inputs;

    public int Probe(string inputPath)
    {
        var ext = Path.GetExtension(inputPath).ToLowerInvariant();
        return SupportedInputs.Contains(ext) ? 100 : 0;
    }

    public async Task<ConversionResult> ConvertAsync(ConversionJob job, CancellationToken ct)
    {
        Interlocked.Increment(ref _calls);
        if (_calls <= _failuresBeforeSuccess)
        {
            await Task.Delay(30, ct);
            return new ConversionResult { Success = false, ErrorCode = "FAIL", ErrorMessage = "fail" };
        }

        var outputs = job.Options.TryGetValue(JobOptionKeys.OutputPaths, out var list) && list is IEnumerable<string> arr
            ? arr.ToList()
            : new List<string> { Path.ChangeExtension(job.InputPath, job.TargetFormat) };
        foreach (var o in outputs)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(o)!);
            await File.WriteAllTextAsync(o, "ok", ct);
        }
        return new ConversionResult { Success = true, OutputPaths = outputs };
    }
}
