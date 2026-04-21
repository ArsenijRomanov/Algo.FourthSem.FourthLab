using HamiltonianPath.Core.Abstractions;
using HamiltonianPath.Core.Domains;
using HamiltonianPath.Core.Enums;
using HamiltonianPath.Core.Helpers;

namespace HamiltonianPath.Core.Strategies;

public class WarnsdorffChooseDirection : IChooseDirection
{
    public (PathState nextState, DirectionFlag chosenDir) GetNextPathState(Board board, PathState pathState)
    {
        var best = default(PathState);
        var bestDir = default(DirectionFlag);
        var bestFreedom = byte.MaxValue;
        var hasBest = false;

        foreach (var dir in StepHelper.All)
        {
            if (!pathState.CanMove(dir) || !board.TryStep(pathState.Point, dir, out var nextPoint))
                continue;

            var nextPathState = new PathState(nextPoint, board.CalculateDirsMask(nextPoint));
            var freedom = nextPathState.AvailableDirectionsCount;

            if (freedom >= bestFreedom)
                continue;

            best = nextPathState;
            bestFreedom = freedom;
            bestDir = dir;
            hasBest = true;
        }

        return !hasBest 
            ? throw new ArgumentException(null, nameof(pathState)) 
            : (best, bestDir);
    }
}
