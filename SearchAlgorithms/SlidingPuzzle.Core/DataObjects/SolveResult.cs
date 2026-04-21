using SlidingPuzzle.Core.Enums;

namespace SlidingPuzzle.Core.DataObjects;

public sealed class SolveResult(IReadOnlyList<Direction> moves, bool isSolved)
{
    public bool IsSolved { get; } = isSolved;
    public IReadOnlyList<Direction> Moves { get; } = moves ?? throw new ArgumentNullException(nameof(moves));
    public int MoveCount => Moves.Count;
}
