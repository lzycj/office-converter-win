using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using OfficeConverter.Core.Models;

namespace OfficeConverter.Core.Runtime;

public interface IConversionDispatcher
{
    event Action<ConversionJob, JobStatus>? JobStatusChanged;
    event Action<ConversionJob, int>? JobAttempt;

    Task<ConversionResult> EnqueueAsync(ConversionJob job, CancellationToken ct = default);
}

public class ConversionDispatcher : IConversionDispatcher
{
    private readonly IEnumerable<IConverter> _converters;
    private readonly ILogger _logger;
    private readonly DispatcherOptions _options;
    private readonly IHasher _hasher;
    private readonly IOutputPathResolver _outputs;
    private readonly IJobLoggerFactory _jobLoggerFactory;

    private readonly ActionBlock<(ConversionJob job, TaskCompletionSource<ConversionResult> tcs)> _worker;

    private readonly ConcurrentDictionary<string, Task<ConversionResult>> _inflight = new();

    public event Action<ConversionJob, JobStatus>? JobStatusChanged;
    public event Action<ConversionJob, int>? JobAttempt;

    public ConversionDispatcher(IEnumerable<IConverter> converters,
        ILogger? logger,
        DispatcherOptions options,
        IHasher hasher,
        IOutputPathResolver outputs,
        IJobLoggerFactory jobLoggerFactory)
    {
        _converters = converters;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        _options = options;
        _hasher = hasher;
        _outputs = outputs;
        _jobLoggerFactory = jobLoggerFactory;

        var execOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, options.Concurrency),
            BoundedCapacity = options.BoundedCapacity
        };

        _worker = new ActionBlock<(ConversionJob job, TaskCompletionSource<ConversionResult> tcs)>(async item =>
        {
            var (job, tcs) = item;
            try
            {
                var res = await ProcessJobAsync(job, CancellationToken.None);
                tcs.TrySetResult(res);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception processing job {JobId}", job.Id);
                tcs.TrySetException(ex);
            }
        }, execOptions);
    }

    public async Task<ConversionResult> EnqueueAsync(ConversionJob job, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(job.InputPath)) throw new ArgumentException("InputPath required", nameof(job));
        if (string.IsNullOrWhiteSpace(job.TargetFormat)) throw new ArgumentException("TargetFormat required", nameof(job));

        job.Hash = await _hasher.ComputeHashAsync(job.InputPath, ct);

        var outputs = _outputs.GetOutputPaths(job.InputPath, job.TargetFormat).ToList();
        job.Options[JobOptionKeys.OutputPaths] = outputs;

        // Skip if outputs exist and are up-to-date
        if (outputs.Count > 0 && outputs.All(p => File.Exists(p)))
        {
            var inputTime = File.GetLastWriteTimeUtc(job.InputPath);
            var newestOutput = outputs.Select(File.GetLastWriteTimeUtc).DefaultIfEmpty(DateTime.MinValue).Max();
            if (newestOutput >= inputTime)
            {
                _logger.LogInformation("Skipping job {JobId} because outputs are up-to-date", job.Id);
                JobStatusChanged?.Invoke(job, JobStatus.Skipped);
                return new ConversionResult
                {
                    Success = true,
                    OutputPaths = outputs,
                    Duration = TimeSpan.Zero
                };
            }
        }

        var key = BuildDedupKey(job);
        if (_inflight.TryGetValue(key, out var existing))
        {
            _logger.LogInformation("Deduplicated job {JobId}, awaiting existing conversion", job.Id);
            return await existing;
        }

        var tcs = new TaskCompletionSource<ConversionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_inflight.TryAdd(key, tcs.Task))
        {
            // Another race: await that
            if (_inflight.TryGetValue(key, out var again))
            {
                return await again;
            }
        }

        var postOk = await _worker.SendAsync((job, tcs), ct);
        if (!postOk)
        {
            _inflight.TryRemove(key, out _);
            throw new InvalidOperationException("Dispatcher rejected job - queue full or completed");
        }

        try
        {
            var res = await tcs.Task;
            return res;
        }
        finally
        {
            _inflight.TryRemove(key, out _);
        }
    }

    private async Task<ConversionResult> ProcessJobAsync(ConversionJob job, CancellationToken outerCt)
    {
        JobStatusChanged?.Invoke(job, JobStatus.Running);

        var candidates = _converters
            .Select(c => (Converter: c, Score: SafeProbe(c, job.InputPath)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Converter)
            .ToList();

        if (candidates.Count == 0)
        {
            JobStatusChanged?.Invoke(job, JobStatus.Failed);
            return new ConversionResult
            {
                Success = false,
                ErrorCode = "NO_CONVERTER",
                ErrorMessage = $"No converter available for {Path.GetExtension(job.InputPath)}"
            };
        }

        var (jobLogger, logPath) = _jobLoggerFactory.Create(job.Id);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(outerCt);
        if (job.Timeout > TimeSpan.Zero)
        {
            cts.CancelAfter(job.Timeout);
        }

        var attempt = 0;
        Exception? lastEx = null;
        ConversionResult? lastResult = null;

        while (attempt < Math.Max(1, _options.MaxAttempts))
        {
            attempt++;
            JobAttempt?.Invoke(job, attempt);
            try
            {
                jobLogger.LogInformation("Starting attempt {Attempt} for job {JobId}", attempt, job.Id);
                var result = await candidates[0].ConvertAsync(job, cts.Token);
                result.LogPath ??= logPath;
                if (result.Success)
                {
                    sw.Stop();
                    result.Duration = sw.Elapsed;
                    JobStatusChanged?.Invoke(job, JobStatus.Succeeded);
                    jobLogger.LogInformation("Job {JobId} succeeded in {Elapsed} on attempt {Attempt}", job.Id, result.Duration, attempt);
                    return result;
                }
                else
                {
                    lastResult = result;
                    jobLogger.LogWarning("Job {JobId} failed on attempt {Attempt}: {ErrorCode} {ErrorMessage}", job.Id, attempt, result.ErrorCode, result.ErrorMessage);
                }
            }
            catch (OperationCanceledException oce)
            {
                sw.Stop();
                JobStatusChanged?.Invoke(job, JobStatus.Failed);
                var r = new ConversionResult
                {
                    Success = false,
                    ErrorCode = cts.IsCancellationRequested ? "CANCELLED" : "TIMEOUT",
                    ErrorMessage = oce.Message,
                    Duration = sw.Elapsed,
                    LogPath = logPath
                };
                jobLogger.LogWarning(oce, "Job {JobId} cancelled or timed out on attempt {Attempt}", job.Id, attempt);
                return r;
            }
            catch (Exception ex)
            {
                lastEx = ex;
                jobLogger.LogError(ex, "Job {JobId} threw on attempt {Attempt}", job.Id, attempt);
            }

            if (attempt < _options.MaxAttempts)
            {
                var delay = TimeSpan.FromMilliseconds(_options.InitialBackoff.TotalMilliseconds * Math.Pow(2, attempt - 1));
                try
                {
                    await Task.Delay(delay, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        sw.Stop();
        JobStatusChanged?.Invoke(job, JobStatus.Failed);
        var failure = lastResult ?? new ConversionResult
        {
            Success = false,
            ErrorCode = lastEx != null ? "EXCEPTION" : "FAILED",
            ErrorMessage = lastEx?.Message
        };
        failure.Duration = sw.Elapsed;
        failure.LogPath ??= logPath;
        return failure;
    }

    private int SafeProbe(IConverter c, string path)
    {
        try { return c.Probe(path); }
        catch { return 0; }
    }

    private static string BuildDedupKey(ConversionJob job)
    {
        var opts = job.Options?.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}") ?? Enumerable.Empty<string>();
        return $"{job.Hash}:{job.TargetFormat}:{string.Join(';', opts)}";
    }
}
