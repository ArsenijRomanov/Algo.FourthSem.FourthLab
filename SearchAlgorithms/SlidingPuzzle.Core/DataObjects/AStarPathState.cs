using SlidingPuzzle.Core.Domains;

namespace SlidingPuzzle.Core.DataObjects;

public record struct AStarPathState
{
    public PuzzleBoard Board { get; private set; }
    public int StepCount { get; private set; }
    public int Score => StepCount + Board.TotalManhattanDistance;

    public AStarPathState(PuzzleBoard board, int stepCount)
    {
        ArgumentNullException.ThrowIfNull(board);
        Board = board;
        StepCount = stepCount;
    }
}
