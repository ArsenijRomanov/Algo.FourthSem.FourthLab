using GridSearch.Core.Contexts;
using GridSearch.Core.Domains;

namespace GridSearch.Core.Abstractions;

public interface ICommitValidator
{
    public bool Validate(SearchContext context, PathState state);
}
