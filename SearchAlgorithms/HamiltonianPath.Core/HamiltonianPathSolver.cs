using HamiltonianPath.Core.Abstractions;
using HamiltonianPath.Core.Contexts;
using HamiltonianPath.Core.Domains;
using HamiltonianPath.Core.Helpers;

namespace HamiltonianPath.Core;

public class HamiltonianPathSolver(
    IChooseDirection chooseDirection, 
    ICommitValidator commitValidator,
    bool backJumpMode)
{
    private IChooseDirection _chooseDirection = chooseDirection ?? throw new ArgumentNullException(nameof(chooseDirection));
    private ICommitValidator _commitValidator = commitValidator ?? throw new ArgumentNullException(nameof(commitValidator));
    private bool _backJumpMode = backJumpMode;

    public bool Solve(Board board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var stack = new Stack<PathState>();
        var startState = new PathState(board.Start, board.CalculateDirsMask(board.Start));
        stack.Push(startState);

        board.SetVisited(board.Start);

        while (stack.Count != 0)
        {
            var curState = stack.Peek();

            if (curState.Point == board.Finish && stack.Count == board.FreePlacesCount)
            {
                BuildSolutionPath(board, stack);
                return true;
            }

            if (curState.AvailableDirectionsCount == 0)
            {
                Backtrack(board, stack);
                continue;
            }

            curState = stack.Pop();

            var (nextState, chosenDir) = _chooseDirection.GetNextPathState(board, curState);
            curState.RemoveDirection(chosenDir);

            stack.Push(curState);

            if (!_commitValidator.Validate(new SearchContext(board, stack.Count), nextState))
                continue;

            board.SetVisited(nextState.Point);
            stack.Push(nextState);
        }

        return false;
    }
    
    public static bool CanHaveHamiltonianPath(Board board)
    {
        ArgumentNullException.ThrowIfNull(board);

        if (board.FreePlacesCount == 0)
            return false;

        if (!board.IsFree(board.Start) || !board.IsFree(board.Finish))
            return false;

        if (board.FreePlacesCount == 1)
            return board.Start == board.Finish;

        if (board.Start == board.Finish)
            return false;

        if (!AreAllFreeCellsConnected(board))
            return false;

        if (board.Height == 1)
            return Math.Abs(board.Start.X - board.Finish.X) == board.FreePlacesCount - 1;

        if (board.Width == 1)
            return Math.Abs(board.Start.Y - board.Finish.Y) == board.FreePlacesCount - 1;

        var blackCount = 0;
        var whiteCount = 0;
        var leafCount = 0;

        for (var y = 0; y < board.Height; y++)
        {
            for (var x = 0; x < board.Width; x++)
            {
                if (board[y, x] != 0)
                    continue;

                var point = new Point(x, y);

                if (((x + y) & 1) == 0)
                    blackCount++;
                else
                    whiteCount++;

                var degree = CountFreeNeighbours(board, point);

                switch (degree)
                {
                    case 0:
                        return false;
                    case 1:
                    {
                        leafCount++;

                        if (point != board.Start && point != board.Finish)
                            return false;
                        break;
                    }
                }
            }
        }

        if (leafCount > 2)
            return false;

        var startBlack = ((board.Start.X + board.Start.Y) & 1) == 0;
        var finishBlack = ((board.Finish.X + board.Finish.Y) & 1) == 0;

        if (startBlack == finishBlack)
        {
            if (Math.Abs(blackCount - whiteCount) != 1)
                return false;

            if (startBlack && blackCount != whiteCount + 1)
                return false;

            if (!startBlack && whiteCount != blackCount + 1)
                return false;
        }
        else
        {
            if (blackCount != whiteCount)
                return false;
        }

        return true;
    }

    private static bool AreAllFreeCellsConnected(Board board)
    {
        Point? firstFree = null;

        for (var y = 0; y < board.Height && firstFree is null; y++)
        {
            for (var x = 0; x < board.Width; x++)
            {
                if (board[y, x] == 0)
                {
                    firstFree = new Point(x, y);
                    break;
                }
            }
        }

        if (firstFree is null)
            return false;

        var visited = new bool[board.Height, board.Width];
        var queue = new Queue<Point>();
        queue.Enqueue(firstFree.Value);
        visited[firstFree.Value.Y, firstFree.Value.X] = true;

        var seen = 0;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            seen++;

            foreach (var dir in DirectionHelper.All)
            {
                var (dx, dy) = DirectionHelper.GetOffset(dir);
                var nx = current.X + dx;
                var ny = current.Y + dy;

                if (!board.Contains(ny, nx))
                    continue;

                if (visited[ny, nx] || board[ny, nx] != 0)
                    continue;

                visited[ny, nx] = true;
                queue.Enqueue(new Point(nx, ny));
            }
        }

        return seen == board.FreePlacesCount;
    }

    private static int CountFreeNeighbours(Board board, Point point)
    {
        var count = 0;

        foreach (var dir in DirectionHelper.All)
        {
            var (dx, dy) = DirectionHelper.GetOffset(dir);
            var nx = point.X + dx;
            var ny = point.Y + dy;

            if (board.Contains(ny, nx) && board[ny, nx] == 0)
                count++;
        }

        return count;
    }
    
    private void Backtrack(Board board, Stack<PathState> stack)
    {
        if (stack.TryPop(out var cur))
        {
            board.Unmark(cur.Point);
            if (!_backJumpMode) return;
        }

        while (stack.TryPeek(out var state) && state.AvailableDirectionsCount == 0)
        {
            cur = stack.Pop();
            board.Unmark(cur.Point);
        }
    }

    private static void BuildSolutionPath(Board board, Stack<PathState> stack)
    {
        var pathLength = stack.Count;

        while (stack.Count != 0)
        {
            var cur = stack.Pop();
            board.Mark(cur.Point, pathLength);
            --pathLength;
        }
    }
}
