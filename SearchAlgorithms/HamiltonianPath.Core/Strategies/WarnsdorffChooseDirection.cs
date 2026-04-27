using HamiltonianPath.Core.Abstractions;
using HamiltonianPath.Core.Domains;
using HamiltonianPath.Core.Enums;
using HamiltonianPath.Core.Helpers;

namespace HamiltonianPath.Core.Strategies;

public class WarnsdorffChooseDirection : IChooseDirection
{
    public bool TryGetNextPathState(
        Board board,
        PathState pathState,
        out PathState nextState,
        out DirectionFlag chosenDir)
    {
        var best = default(PathState);
        var bestDir = DirectionFlag.None;
        var bestFreedom = byte.MaxValue;
        var hasBest = false;

        foreach (var dir in StepHelper.All)
        {
            if (!pathState.CanMove(dir) || !board.TryStep(pathState.Point, dir, out var nextPoint))
                continue;

            var candidate = new PathState(nextPoint, board.CalculateDirsMask(nextPoint));
            var freedom = candidate.AvailableDirectionsCount;

            if (freedom >= bestFreedom)
                continue;

            best = candidate;
            bestDir = dir;
            bestFreedom = freedom;
            hasBest = true;
        }

        nextState = best;
        chosenDir = bestDir;
        return hasBest;
    }
}
