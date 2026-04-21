using HamiltonianPath.Core.Domains;

namespace HamiltonianPath.Core.Abstractions;

public interface IChooseDirection
{
    (PathState nextState, Direction chosenDir) GetNextPathState(Board matrix, PathState pathState);
}
