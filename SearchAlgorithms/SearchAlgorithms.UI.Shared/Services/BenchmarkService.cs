using System.Diagnostics;
using SearchAlgorithms.UI.Shared.Models;

namespace SearchAlgorithms.UI.Shared.Services;

public sealed class BenchmarkService
{
    public BenchmarkRunResult<TResult> Run<TResult>(Func<TResult> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var process = Process.GetCurrentProcess();
        var managedBefore = GC.GetTotalMemory(forceFullCollection: true);
        var workingSetBefore = process.WorkingSet64;

        var stopwatch = Stopwatch.StartNew();
        var result = action();
        stopwatch.Stop();

        process.Refresh();
        var managedAfter = GC.GetTotalMemory(forceFullCollection: true);
        var workingSetAfter = process.WorkingSet64;

        return new BenchmarkRunResult<TResult>
        {
            Result = result,
            Elapsed = stopwatch.Elapsed,
            ManagedMemoryDeltaBytes = Math.Max(0, managedAfter - managedBefore),
            WorkingSetDeltaBytes = Math.Max(0, workingSetAfter - workingSetBefore)
        };
    }
}
