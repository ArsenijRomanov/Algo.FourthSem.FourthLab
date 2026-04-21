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

        if (!TryGetAnyAvailableDirection(state, out var firstDir))
            return true;
        
        EnsureStampBuffer(board.Height, board.Width);
        StartNewStamp();
        
        if (!TryGetNeighbour(board, state.Point, firstDir, out var firstNeighbour))
            return false;

        MarkConnectedComponent(board, firstNeighbour, state.Point);

        foreach (var dir in StepHelper.All)
        {
            if (!state.CanMove(dir))
                continue;

            if (!TryGetNeighbour(board, state.Point, dir, out var neighbour))
                return false;

            if (!IsMarked(neighbour))
                return false;
        }

        return true;
    }

    private void MarkConnectedComponent(Board board, Point start, Point blockedPoint)
    {
        var queue = new Queue<Point>();

        Mark(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var dir in StepHelper.All)
            {
                if (!TryGetFreeNeighbour(board, current, blockedPoint, dir, out var next))
                    continue;

                Mark(next);
                queue.Enqueue(next);
            }
        }
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
        var candidate = new Point(nextX, nextY);

        if (candidate == blockedPoint)
        {
            nextPoint = default;
            return false;
        }

        if (board.Contains(nextY, nextX) && board[nextY, nextX] == 0 && !IsMarked(candidate))
        {
            nextPoint = candidate;
            return true;
        }

        nextPoint = default;
        return false;
    }

    private static bool TryGetNeighbour(Board board, Point point, DirectionFlag dir, out Point neighbour)
    {
        var (dx, dy) = StepHelper.GetOffset(dir);
        var nextX = point.X + dx;
        var nextY = point.Y + dy;

        if (!board.Contains(nextY, nextX))
        {
            neighbour = default;
            return false;
        }

        neighbour = new Point(nextX, nextY);
        return true;
    }

    private static bool TryGetAnyAvailableDirection(PathState state, out DirectionFlag dir)
    {
        foreach (var candidate in StepHelper.All)
        {
            if (!state.CanMove(candidate))
                continue;

            dir = candidate;
            return true;
        }

        dir = DirectionFlag.None;
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
