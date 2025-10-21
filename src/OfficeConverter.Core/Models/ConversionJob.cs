using System;
using System.Collections.Generic;

namespace OfficeConverter.Core.Models;

public class ConversionJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string InputPath { get; set; } = string.Empty;
    public string TargetFormat { get; set; } = string.Empty;
    public Dictionary<string, object> Options { get; set; } = new();
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
    public int Priority { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Hash { get; set; } = string.Empty;
}
