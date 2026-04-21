using HamiltonianPath.Core.Domains;
using HamiltonianPath.Core.Enums;

namespace HamiltonianPath.Core.Abstractions;

public interface IChooseDirection
{
    (PathState nextState, DirectionFlag chosenDir) GetNextPathState(Board matrix, PathState pathState);
}
