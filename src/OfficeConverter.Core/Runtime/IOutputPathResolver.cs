using System.Collections.Generic;

namespace OfficeConverter.Core.Runtime;

public interface IOutputPathResolver
{
    /// <summary>
    /// Given an input file path and target format, return one or more output file paths that should be produced.
    /// </summary>
    IEnumerable<string> GetOutputPaths(string inputPath, string targetFormat);
}
