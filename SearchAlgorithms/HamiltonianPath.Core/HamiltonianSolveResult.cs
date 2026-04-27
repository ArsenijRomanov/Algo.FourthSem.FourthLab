using HamiltonianPath.Core.Domains;

namespace HamiltonianPath.Core;

public sealed class HamiltonianSolveResult
{
    public long SolutionCount { get; }
    public IReadOnlyList<Point>? FirstSolution { get; }
    public bool HasSolution => SolutionCount > 0;

    public HamiltonianSolveResult(long solutionCount, IReadOnlyList<Point>? firstSolution)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(solutionCount);
        SolutionCount = solutionCount;
        FirstSolution = firstSolution;
    }
}
