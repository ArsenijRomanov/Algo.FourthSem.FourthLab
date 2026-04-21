using GridSearch.Core.Domains;

namespace GridSearch.Core.Contexts;

public struct SearchContext
{
    public readonly Board Board;
    public readonly int PathLength;

    public SearchContext(Board board, int pathLength)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentOutOfRangeException.ThrowIfNegative(pathLength);

        Board = board;
        PathLength = pathLength;
    }
}
