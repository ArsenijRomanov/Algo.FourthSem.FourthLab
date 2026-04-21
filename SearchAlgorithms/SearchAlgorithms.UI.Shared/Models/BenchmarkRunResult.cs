namespace SearchAlgorithms.UI.Shared.Models;

public sealed class BenchmarkRunResult<TResult>
{
    public required TResult Result { get; init; }
    public required TimeSpan Elapsed { get; init; }
    public required long ManagedMemoryDeltaBytes { get; init; }
    public required long WorkingSetDeltaBytes { get; init; }
}
