using SlidingPuzzle.Core.Abstractions;
using SlidingPuzzle.Core.DataObjects;
using SlidingPuzzle.Core.Domains;
using SlidingPuzzle.Core.Enums;
using SlidingPuzzle.Core.Helpers;

namespace SlidingPuzzle.Core.Solvers;

public class IdaSolver : ISolver
{
    private const int Found = -1;

    public SolveResult Solve(PuzzleBoard board, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(board);

        var workBoard = new PuzzleBoard(board);
        var path = new List<Direction>();
        var pathVisited = new HashSet<PuzzleBoardKey> { workBoard.GetKey() };

        var bound = workBoard.TotalManhattanDistance;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nextBound = Search(workBoard, 0, bound, null, pathVisited, path, cancellationToken);

            if (nextBound == Found)
                return new SolveResult(path.ToArray(), true);

            if (nextBound == int.MaxValue)
                return new SolveResult([], false);

            bound = nextBound;
        }
    }

    private static int Search(
        PuzzleBoard board,
        int stepCount,
        int bound,
        Direction? previousDirection,
        HashSet<PuzzleBoardKey> pathVisited,
        List<Direction> path,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var score = stepCount + board.TotalManhattanDistance;
        if (score > bound)
            return score;

        if (board.IsGoal)
            return Found;

        var minExceededScore = int.MaxValue;

        foreach (var dir in board.GetValidSteps())
        {
            if (previousDirection.HasValue &&
                dir == DirectionHelper.GetOppositeDirection(previousDirection.Value))
                continue;

            board.ApplyStep(dir);
            var key = board.GetKey();

            if (!pathVisited.Add(key))
            {
                board.UndoStep(dir);
                continue;
            }

            path.Add(dir);

            var searchResult = Search(
                board,
                stepCount + 1,
                bound,
                dir,
                pathVisited,
                path,
                cancellationToken);

            if (searchResult == Found)
                return Found;

            if (searchResult < minExceededScore)
                minExceededScore = searchResult;

            path.RemoveAt(path.Count - 1);
            pathVisited.Remove(key);
            board.UndoStep(dir);
        }

        return minExceededScore;
    }
}
