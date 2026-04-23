using SlidingPuzzle.Core.DataObjects;
using SlidingPuzzle.Core.Domains;

namespace SlidingPuzzle.Core.Abstractions;

public interface ISolver
{
    SolveResult Solve(PuzzleBoard board, CancellationToken cancellationToken = default);
}
