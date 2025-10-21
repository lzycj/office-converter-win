using System;

namespace OfficeConverter.Core.Runtime;

public class DispatcherOptions
{
    public int Concurrency { get; set; } = 2;
    public int BoundedCapacity { get; set; } = 128;
    public int MaxAttempts { get; set; } = 3;
    public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromSeconds(0.5);
}
