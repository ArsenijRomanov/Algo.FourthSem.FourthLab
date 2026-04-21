using GridSearch.Core.Abstractions;
using GridSearch.Core.Contexts;
using GridSearch.Core.Domains;

namespace GridSearch.Core;

public class GridSearchSolver(
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

            if (curState.Point == board.Finish)
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
        if (board.FreePlacesCount == 0)
            return false;

        if (board[board.Start] != 0 || board[board.Finish] != 0)
            return false;

        if (board.FreePlacesCount == 1)
            return board.Start == board.Finish;

        if (board.Start == board.Finish)
            return false;
        
        if (board.Height == 1 && int.Abs(board.Start.X - board.Finish.X) != board.FreePlacesCount - 1) return false;
        if (board.Width == 1 && int.Abs(board.Start.Y - board.Finish.Y) != board.FreePlacesCount - 1) return false;
        
        var evenPlaces = board.FreePlacesCount % 2 == 0;
        var evenManhattanDistance = (board.Start.X - board.Finish.X + board.Start.Y - board.Finish.Y) % 2 == 0;

        return evenPlaces ^ evenManhattanDistance;
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

    private void BuildSolutionPath(Board board, Stack<PathState> stack)
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
