using GridSearch.Core.Abstractions;
using GridSearch.Core.Contexts;
using GridSearch.Core.Domains;
using GridSearch.Core.Helpers;

namespace GridSearch.Core.Strategies;

public class ConnectivityCommitValidator : ICommitValidator
{
    public bool Validate(SearchContext context, PathState state)
    {
        var board = context.Board;
        var isLastStep = context.PathLength == board.Height * board.Width - 1;

        if (state.Point == board.Finish != isLastStep)
            return false;

        if (state.AvailableDirectionsCount < 2)
            return true;

        if (!TryGetAnyAvailableDirection(state, out var firstDir))
            return true;

        var marked = new List<Point>();

        board.SetVisited(state.Point);

        try
        {
            var firstNeighbour = GetNeighbourUnchecked(board, state.Point, firstDir);

            MarkConnectedComponent(board, firstNeighbour, marked);

            foreach (var dir in DirectionHelper.All)
            {
                if (!state.CanMove(dir))
                    continue;

                var neighbour = GetNeighbourUnchecked(board, state.Point, dir);

                if (board[neighbour] != -1)
                    return false;
            }

            return true;
        }
        finally
        {
            foreach (var point in marked)
                board.Unmark(point);

            board.Unmark(state.Point);
        }
    }

    private static void MarkConnectedComponent(Board board, Point start, List<Point> marked)
    {
        var queue = new Queue<Point>();

        board.Mark(start, -1);
        marked.Add(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var dir in DirectionHelper.All)
            {
                if (!TryGetFreeNeighbour(board, current, dir, out var next))
                    continue;

                board.Mark(next, -1);
                marked.Add(next);
                queue.Enqueue(next);
            }
        }
    }

    private static bool TryGetFreeNeighbour(Board board, Point point, Direction dir, out Point nextPoint)
    {
        var (dx, dy) = DirectionHelper.GetOffset(dir);
        var nextX = point.X + dx;
        var nextY = point.Y + dy;

        if (board.Contains(nextY, nextX) && board[nextY, nextX] == 0)
        {
            nextPoint = new Point(nextX, nextY);
            return true;
        }

        nextPoint = default;
        return false;
    }

    private static Point GetNeighbourUnchecked(Board board, Point point, Direction dir)
    {
        var (dx, dy) = DirectionHelper.GetOffset(dir);
        var nextX = point.X + dx;
        var nextY = point.Y + dy;

        return !board.Contains(nextY, nextX) 
            ? throw new ArgumentException(null, nameof(dir)) 
            : new Point(nextX, nextY);
    }

    private static bool TryGetAnyAvailableDirection(PathState state, out Direction dir)
    {
        foreach (var candidate in DirectionHelper.All)
        {
            if (!state.CanMove(candidate))
                continue;

            dir = candidate;
            return true;
        }

        dir = Direction.None;
        return false;
    }
}
