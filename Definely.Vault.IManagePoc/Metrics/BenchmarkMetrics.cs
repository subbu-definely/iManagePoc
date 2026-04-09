using System.Diagnostics;

namespace Definely.Vault.IManagePoc.Metrics;

public class BenchmarkMetrics
{
    private readonly Stopwatch _stopwatch = new();
    private readonly Dictionary<string, int> _apiCalls = new();
    private long _peakMemory;

    public string RunId { get; } = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}";

    public void Start()
    {
        _stopwatch.Restart();
        TrackMemory();
    }

    public void Stop()
    {
        _stopwatch.Stop();
        TrackMemory();
    }

    public double ElapsedSeconds => _stopwatch.Elapsed.TotalSeconds;
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public void IncrementApiCall(string callType)
    {
        if (!_apiCalls.ContainsKey(callType))
            _apiCalls[callType] = 0;
        _apiCalls[callType]++;
        TrackMemory();
    }

    public int GetApiCallCount(string callType) =>
        _apiCalls.TryGetValue(callType, out var count) ? count : 0;

    public int TotalApiCalls => _apiCalls.Values.Sum();

    public double PeakMemoryMb => _peakMemory / (1024.0 * 1024.0);

    private void TrackMemory()
    {
        var current = GC.GetTotalMemory(false);
        if (current > _peakMemory)
            _peakMemory = current;
    }

    public void PrintSummary(string scenario)
    {
        Console.WriteLine();
        Console.WriteLine($"=== Benchmark Results: {scenario} ===");
        Console.WriteLine($"Run ID:         {RunId}");
        Console.WriteLine($"Elapsed:        {Elapsed:hh\\:mm\\:ss\\.fff}");
        Console.WriteLine($"Total API calls: {TotalApiCalls}");
        Console.WriteLine($"Peak memory:    {PeakMemoryMb:F1} MB");
        Console.WriteLine();
        Console.WriteLine("API call breakdown:");
        foreach (var (callType, count) in _apiCalls.OrderByDescending(x => x.Value))
        {
            Console.WriteLine($"  {callType,-40} {count,8}");
        }
        Console.WriteLine();
    }
}
