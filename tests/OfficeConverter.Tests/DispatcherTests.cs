using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OfficeConverter.Core.Models;
using OfficeConverter.Core.Runtime;
using OfficeConverter.Infrastructure.Config;
using OfficeConverter.Infrastructure.Hashing;
using OfficeConverter.Infrastructure.Logging;
using OfficeConverter.Infrastructure.Output;
using OfficeConverter.Tests.Fakes;
using Xunit;

namespace OfficeConverter.Tests;

public class DispatcherTests
{
    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "OfficeConverter.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task Retries_failed_jobs_with_exponential_backoff()
    {
        var workDir = CreateTempDir();
        var input = Path.Combine(workDir, "file.txt");
        await File.WriteAllTextAsync(input, "content");

        var converter = new FakeConverter(failuresBeforeSuccess: 2, ".txt");
        var hasher = new Sha256Hasher();
        var cfg = new AppConfig { OutputDir = Path.Combine(workDir, "out") };
        var outputs = new DefaultOutputPathResolver(cfg);
        var (lf, logsDir) = LoggingBootstrapper.Initialize(workDir);
        var jobLoggerFactory = new JobLoggerFactory(logsDir);
        var options = new DispatcherOptions { Concurrency = 1, MaxAttempts = 3, InitialBackoff = TimeSpan.FromMilliseconds(50) };
        var dispatcher = new ConversionDispatcher(new[] { converter }, NullLogger.Instance, options, hasher, outputs, jobLoggerFactory);

        var job = new ConversionJob { InputPath = input, TargetFormat = "pdf", Timeout = TimeSpan.FromSeconds(5) };
        var result = await dispatcher.EnqueueAsync(job, CancellationToken.None);

        result.Success.Should().BeTrue();
        converter.Calls.Should().Be(3);
        File.Exists(result.OutputPaths.First()).Should().BeTrue();
    }

    [Fact]
    public async Task Deduplicates_same_input_by_hash()
    {
        var workDir = CreateTempDir();
        var input = Path.Combine(workDir, "file.txt");
        await File.WriteAllTextAsync(input, "same content");

        var converter = new FakeConverter(failuresBeforeSuccess: 0, ".txt");
        var hasher = new Sha256Hasher();
        var cfg = new AppConfig { OutputDir = Path.Combine(workDir, "out") };
        var outputs = new DefaultOutputPathResolver(cfg);
        var (lf, logsDir) = LoggingBootstrapper.Initialize(workDir);
        var jobLoggerFactory = new JobLoggerFactory(logsDir);
        var options = new DispatcherOptions { Concurrency = 2, MaxAttempts = 1 };
        var dispatcher = new ConversionDispatcher(new[] { converter }, NullLogger.Instance, options, hasher, outputs, jobLoggerFactory);

        var job1 = new ConversionJob { InputPath = input, TargetFormat = "pdf" };
        var job2 = new ConversionJob { InputPath = input, TargetFormat = "pdf" };

        var t1 = dispatcher.EnqueueAsync(job1);
        var t2 = dispatcher.EnqueueAsync(job2);
        var results = await Task.WhenAll(t1, t2);

        results.All(r => r.Success).Should().BeTrue();
        converter.Calls.Should().Be(1);
        results[0].OutputPaths.First().Should().Be(results[1].OutputPaths.First());
    }
}
