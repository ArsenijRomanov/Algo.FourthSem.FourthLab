using GridSearch.Core.Abstractions;
using GridSearch.Core.Contexts;
using GridSearch.Core.Domains;

namespace GridSearch.Core.Strategies;

public class BaseCommitValidator : ICommitValidator
{
    public bool Validate(SearchContext context, PathState state)
    {
        var isLastStep = context.PathLength == context.Board.Height * context.Board.Width - 1;
        return state.Point == context.Board.Finish == isLastStep;
    }
}
