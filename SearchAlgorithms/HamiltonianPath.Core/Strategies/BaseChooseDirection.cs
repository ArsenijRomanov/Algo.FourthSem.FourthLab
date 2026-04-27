using HamiltonianPath.Core.Abstractions;
using HamiltonianPath.Core.Domains;
using HamiltonianPath.Core.Enums;
using HamiltonianPath.Core.Helpers;

namespace HamiltonianPath.Core.Strategies;

public class BaseChooseDirection : IChooseDirection
{
    public bool TryGetNextPathState(
        Board board,
        PathState pathState,
        out PathState nextState,
        out DirectionFlag chosenDir)
    {
        foreach (var dir in StepHelper.All)
        {
            if (!pathState.CanMove(dir) || !board.TryStep(pathState.Point, dir, out var nextPoint))
                continue;

            nextState = new PathState(nextPoint, board.CalculateDirsMask(nextPoint));
            chosenDir = dir;
            return true;
        }

        nextState = default;
        chosenDir = DirectionFlag.None;
        return false;
    }
}
