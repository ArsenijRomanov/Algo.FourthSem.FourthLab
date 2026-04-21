using HamiltonianPath.Core.Abstractions;
using HamiltonianPath.Core.Contexts;
using HamiltonianPath.Core.Domains;

namespace HamiltonianPath.Core.Strategies;

public class BaseCommitValidator : ICommitValidator
{
    public bool Validate(SearchContext context, PathState state)
    {
        var isLastStep = context.PathLength == context.Board.FreePlacesCount - 1;
        return state.Point == context.Board.Finish == isLastStep;
    }
}
