using HamiltonianPath.Core.Contexts;
using HamiltonianPath.Core.Domains;

namespace HamiltonianPath.Core.Abstractions;

public interface ICommitValidator
{
    public bool Validate(SearchContext context, PathState state);
}
