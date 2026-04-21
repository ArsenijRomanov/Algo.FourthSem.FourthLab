using SlidingPuzzle.Core.Abstractions;
using SlidingPuzzle.Core.DataObjects;
using SlidingPuzzle.Core.Domains;
using SlidingPuzzle.Core.Enums;
using SlidingPuzzle.Core.Helpers;

namespace SlidingPuzzle.Core.Solvers;

public class DfsBackJumpSolver : ISolver
{
    public SolveResult Solve(PuzzleBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var workBoard = new PuzzleBoard(board);
        var path = new List<Direction>();
        var pathVisited = new HashSet<PuzzleBoardKey> { workBoard.GetKey() };

        var isSolved = Search(workBoard, null, pathVisited, path);

        return isSolved
            ? new SolveResult(path.ToArray(), true)
            : new SolveResult([], false);
    }

    private static bool Search(
        PuzzleBoard board,
        Direction? previousMove,
        HashSet<PuzzleBoardKey> pathVisited,
        List<Direction> path)
    {
        if (board.IsGoal)
            return true;

        var moves = GetOrderedMoves(board, previousMove);

        foreach (var dir in moves)
        {
            board.ApplyStep(dir);
            var key = board.GetKey();

            if (!pathVisited.Add(key))
            {
                board.UndoStep(dir);
                continue;
            }

            path.Add(dir);

            if (Search(board, dir, pathVisited, path))
                return true;

            path.RemoveAt(path.Count - 1);
            pathVisited.Remove(key);
            board.UndoStep(dir);
        }

        return false;
    }

    private static List<Direction> GetOrderedMoves(PuzzleBoard board, Direction? previousMove)
    {
        var moves = new List<(Direction dir, int score)>();

        foreach (var dir in board.GetValidSteps())
        {
            if (previousMove.HasValue && dir == DirectionHelper.GetOppositeDirection(previousMove.Value))
                continue;

            var nextBoard = board.MakeStep(dir);
            moves.Add((dir, nextBoard.TotalManhattanDistance));
        }

        moves.Sort((left, right) => left.score.CompareTo(right.score));

        var result = new List<Direction>(moves.Count);
        foreach (var move in moves)
            result.Add(move.dir);

        return result;
    }
}
