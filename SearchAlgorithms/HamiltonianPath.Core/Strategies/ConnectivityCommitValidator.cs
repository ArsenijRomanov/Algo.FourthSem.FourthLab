using HamiltonianPath.Core.Abstractions;
using HamiltonianPath.Core.Contexts;
using HamiltonianPath.Core.Domains;
using HamiltonianPath.Core.Enums;
using HamiltonianPath.Core.Helpers;

namespace HamiltonianPath.Core.Strategies;

public class ConnectivityCommitValidator : ICommitValidator
{
    private int[,]? _visitStamp;
    private int _currentStamp;

    public bool Validate(SearchContext context, PathState state)
    {
        var board = context.Board;
        var isLastStep = context.PathLength == context.Board.FreePlacesCount - 1;

        if (state.Point == board.Finish != isLastStep)
            return false;

        if (state.AvailableDirectionsCount < 2)
            return true;

        var remainingFreeCells = board.FreePlacesCount - context.PathLength - 1;
        if (remainingFreeCells <= 0)
            return true;

        if (!TryGetAnyAvailableNeighbour(board, state, out var firstNeighbour))
            return true;

        EnsureStampBuffer(board.Height, board.Width);
        StartNewStamp();

        var reachable = MarkConnectedComponent(board, firstNeighbour, state.Point);
        return reachable == remainingFreeCells;
    }

    private int MarkConnectedComponent(Board board, Point start, Point blockedPoint)
    {
        var queue = new Queue<Point>();
        var markedCount = 0;

        Mark(start);
        markedCount++;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var dir in StepHelper.All)
            {
                if (!TryGetFreeNeighbour(board, current, blockedPoint, dir, out var next))
                    continue;

                Mark(next);
                markedCount++;
                queue.Enqueue(next);
            }
        }

        return markedCount;
    }

    private bool TryGetFreeNeighbour(
        Board board,
        Point point,
        Point blockedPoint,
        DirectionFlag dir,
        out Point nextPoint)
    {
        var (dx, dy) = StepHelper.GetOffset(dir);
        var nextX = point.X + dx;
        var nextY = point.Y + dy;

        if (!board.Contains(nextY, nextX))
        {
            nextPoint = default;
            return false;
        }

        var candidate = new Point(nextX, nextY);

        if (candidate == blockedPoint || board[nextY, nextX] != 0 || IsMarked(candidate))
        {
            nextPoint = default;
            return false;
        }

        nextPoint = candidate;
        return true;
    }

    private static bool TryGetAnyAvailableNeighbour(Board board, PathState state, out Point neighbour)
    {
        foreach (var dir in StepHelper.All)
        {
            if (!state.CanMove(dir))
                continue;

            if (board.TryStep(state.Point, dir, out neighbour))
                return true;
        }

        neighbour = default;
        return false;
    }

    private void EnsureStampBuffer(int height, int width)
    {
        if (_visitStamp is not null &&
            _visitStamp.GetLength(0) == height &&
            _visitStamp.GetLength(1) == width)
            return;

        _visitStamp = new int[height, width];
        _currentStamp = 0;
    }

    private void StartNewStamp()
    {
        _currentStamp++;

        if (_visitStamp is null || _currentStamp != int.MaxValue)
            return;

        Array.Clear(_visitStamp);
        _currentStamp = 1;
    }

    private void Mark(Point point)
        => _visitStamp![point.Y, point.X] = _currentStamp;

    private bool IsMarked(Point point)
        => _visitStamp![point.Y, point.X] == _currentStamp;
}
