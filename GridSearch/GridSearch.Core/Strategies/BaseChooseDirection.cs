using GridSearch.Core.Abstractions;
using GridSearch.Core.Domains;
using GridSearch.Core.Helpers;

namespace GridSearch.Core.Strategies;

public class BaseChooseDirection : IChooseDirection
{
    public (PathState nextState, Direction chosenDir) GetNextPathState(Board board, PathState pathState)
    {
        foreach (var dir in DirectionHelper.All)
        {
            if (!pathState.CanMove(dir) || !board.TryStep(pathState.Point, dir, out var nextPoint)) continue;
            var nextPathState = new PathState(nextPoint, board.CalculateDirsMask(nextPoint));
            return (nextPathState, dir);
        }

        throw new ArgumentException(null, nameof(pathState));
    }
}
