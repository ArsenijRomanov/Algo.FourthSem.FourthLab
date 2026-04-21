using GridSearch.Core.Domains;

namespace GridSearch.Core.Abstractions;

public interface IChooseDirection
{
    (PathState nextState, Direction chosenDir) GetNextPathState(Board matrix, PathState pathState);
}
