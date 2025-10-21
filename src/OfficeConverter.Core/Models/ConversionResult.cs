using System;
using System.Collections.Generic;

namespace OfficeConverter.Core.Models;

public class ConversionResult
{
    public bool Success { get; set; }
    public List<string> OutputPaths { get; set; } = new();
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
    public string? LogPath { get; set; }
}
