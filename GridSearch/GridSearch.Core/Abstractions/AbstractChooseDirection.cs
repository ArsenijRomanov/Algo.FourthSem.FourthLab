using GridSearch.Core.Domains;

namespace GridSearch.Core.Abstractions;

public abstract class AbstractChooseDirection : IChooseDirection
{
    protected static ReadOnlySpan<Direction> Directions =>
    [
        Direction.Left,
        Direction.Down,
        Direction.Right,
        Direction.Up
    ];
    
    public abstract PathState GetNextPathState(Board board, PathState pathState);
}
