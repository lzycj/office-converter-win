using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OfficeConverter.Core.Models;

namespace OfficeConverter.Core;

public interface IConverter
{
    IEnumerable<string> SupportedInputs { get; }

    /// <summary>
    /// Returns a score 0-100 indicating how suitable this converter is for the given input path.
    /// </summary>
    int Probe(string inputPath);

    /// <summary>
    /// Perform the conversion for the specified job.
    /// </summary>
    Task<ConversionResult> ConvertAsync(ConversionJob job, CancellationToken ct);
}
