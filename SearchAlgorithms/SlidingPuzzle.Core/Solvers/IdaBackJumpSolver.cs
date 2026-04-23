using SlidingPuzzle.Core.Abstractions;
using SlidingPuzzle.Core.DataObjects;
using SlidingPuzzle.Core.Domains;
using SlidingPuzzle.Core.Enums;
using SlidingPuzzle.Core.Helpers;

namespace SlidingPuzzle.Core.Solvers;

public class IdaBackJumpSolver : ISolver
{
    private readonly record struct SearchResult(int NextBound, int JumpDepth, bool IsFound);

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
            var searchResult = Search(
                workBoard,
                stepCount: 0,
                bound,
                previousDirection: null,
                lastChoiceDepth: 0,
                pathVisited,
                path,
                cancellationToken);

            if (searchResult.IsFound)
                return new SolveResult(path.ToArray(), true);

            if (searchResult.NextBound == int.MaxValue)
                return new SolveResult([], false);

            bound = searchResult.NextBound;
        }
    }

    private static SearchResult Search(
        PuzzleBoard board,
        int stepCount,
        int bound,
        Direction? previousDirection,
        int lastChoiceDepth,
        HashSet<PuzzleBoardKey> pathVisited,
        List<Direction> path,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var score = stepCount + board.TotalManhattanDistance;

        if (score > bound)
            return new SearchResult(score, lastChoiceDepth, false);

        if (board.IsGoal)
            return new SearchResult(stepCount, stepCount, true);

        var moves = GetAvailableMoves(board, previousDirection, pathVisited);

        if (moves.Count == 0)
            return new SearchResult(int.MaxValue, lastChoiceDepth, false);

        var nextLastChoiceDepth = moves.Count > 1
            ? stepCount
            : lastChoiceDepth;

        var minExceededScore = int.MaxValue;

        foreach (var dir in moves)
        {
            board.ApplyStep(dir);
            var key = board.GetKey();

            pathVisited.Add(key);
            path.Add(dir);

            var searchResult = Search(
                board,
                stepCount + 1,
                bound,
                dir,
                nextLastChoiceDepth,
                pathVisited,
                path,
                cancellationToken);

            if (searchResult.IsFound)
                return searchResult;

            if (searchResult.NextBound < minExceededScore)
                minExceededScore = searchResult.NextBound;

            path.RemoveAt(path.Count - 1);
            pathVisited.Remove(key);
            board.UndoStep(dir);

            if (searchResult.JumpDepth < stepCount)
                return new SearchResult(minExceededScore, searchResult.JumpDepth, false);
        }

        return new SearchResult(minExceededScore, lastChoiceDepth, false);
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
