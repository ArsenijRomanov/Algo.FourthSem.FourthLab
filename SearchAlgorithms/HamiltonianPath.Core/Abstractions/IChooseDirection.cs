using HamiltonianPath.Core.Domains;
using HamiltonianPath.Core.Enums;

namespace HamiltonianPath.Core.Abstractions;

public interface IChooseDirection
{
    bool TryGetNextPathState(
        Board board,
        PathState pathState,
        out PathState nextState,
        out DirectionFlag chosenDir);
}
