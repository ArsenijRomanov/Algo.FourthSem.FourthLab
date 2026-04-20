using GridSearch.Core.Abstractions;
using GridSearch.Core.Domains;
using GridSearch.Core.Helpers;

namespace GridSearch.Core.Strategies;

public class WarnsdorffChooseDirection : IChooseDirection
{
    public PathState GetNextPathState(Board board, PathState pathState)
    {
        var best = default(PathState);
        var bestFreedom = byte.MaxValue;
        var hasBest = false;

        foreach (var dir in DirectionHelper.All)
        {
            if (!pathState.CanMove(dir) || !board.TryStep(pathState.Point, dir, out var nextPoint))
                continue;

            var nextPathState = new PathState(nextPoint, board.CalculateDirsMask(nextPoint));
            var freedom = nextPathState.AvailableDirectionsCount;

            if (freedom >= bestFreedom)
                continue;

            best = nextPathState;
            bestFreedom = freedom;
            hasBest = true;
        }

        return hasBest
            ? best
            : throw new ArgumentException(null, nameof(pathState));
    }
}
