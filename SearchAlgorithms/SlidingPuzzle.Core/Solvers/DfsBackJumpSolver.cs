using SlidingPuzzle.Core.Abstractions;
using SlidingPuzzle.Core.DataObjects;
using SlidingPuzzle.Core.Domains;
using SlidingPuzzle.Core.Enums;
using SlidingPuzzle.Core.Helpers;

namespace SlidingPuzzle.Core.Solvers;

public class DfsBackJumpSolver : ISolver
{
    private const int Found = -1;

    public SolveResult Solve(PuzzleBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var workBoard = new PuzzleBoard(board);
        var path = new List<Direction>();
        var pathVisited = new HashSet<PuzzleBoardKey> { workBoard.GetKey() };

        var searchResult = Search(
            workBoard,
            null,
            depth: 0,
            lastChoiceDepth: 0,
            pathVisited,
            path);

        return searchResult == Found
            ? new SolveResult(path.ToArray(), true)
            : new SolveResult([], false);
    }

    private static int Search(
        PuzzleBoard board,
        Direction? previousDirection,
        int depth,
        int lastChoiceDepth,
        HashSet<PuzzleBoardKey> pathVisited,
        List<Direction> path)
    {
        if (board.IsGoal)
            return Found;

        var moves = GetAvailableMoves(board, previousDirection, pathVisited);

        if (moves.Count == 0)
            return lastChoiceDepth;

        var nextLastChoiceDepth = moves.Count > 1
            ? depth
            : lastChoiceDepth;

        foreach (var dir in moves)
        {
            board.ApplyStep(dir);
            var key = board.GetKey();

            pathVisited.Add(key);
            path.Add(dir);

            var searchResult = Search(
                board,
                dir,
                depth + 1,
                nextLastChoiceDepth,
                pathVisited,
                path);

            if (searchResult == Found)
                return Found;

            path.RemoveAt(path.Count - 1);
            pathVisited.Remove(key);
            board.UndoStep(dir);

            if (searchResult < depth)
                return searchResult;
        }

        return lastChoiceDepth;
    }

    private static List<Direction> GetAvailableMoves(
        PuzzleBoard board,
        Direction? previousDirection,
        HashSet<PuzzleBoardKey> pathVisited)
    {
        var moves = new List<(Direction dir, int score)>();

        foreach (var dir in board.GetValidSteps())
        {
            if (previousDirection.HasValue &&
                dir == DirectionHelper.GetOppositeDirection(previousDirection.Value))
                continue;

            var nextBoard = board.MakeStep(dir);
            var key = nextBoard.GetKey();

            if (pathVisited.Contains(key))
                continue;

            moves.Add((dir, nextBoard.TotalManhattanDistance));
        }

        moves.Sort((left, right) =>
        {
            var scoreComparison = left.score.CompareTo(right.score);
            return scoreComparison != 0 
                ? scoreComparison 
                : left.dir.CompareTo(right.dir);
        });

        var result = new List<Direction>(moves.Count);

        foreach (var move in moves)
            result.Add(move.dir);

        return result;
    }
}