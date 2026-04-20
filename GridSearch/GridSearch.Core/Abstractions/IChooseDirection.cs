using GridSearch.Core.Domains;

namespace GridSearch.Core.Abstractions;

public interface IChooseDirection
{
    PathState GetNextPathState(Board matrix, PathState pathState);
}
