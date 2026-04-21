using SlidingPuzzle.Core.Enums;

namespace SlidingPuzzle.Core.Helpers;

public static class DirectionHelper
{
    public static ReadOnlySpan<Direction> All =>
    [
        Direction.Left,
        Direction.Down,
        Direction.Right,
        Direction.Up
    ];
    
    public static Direction GetOppositeDirection(Direction dir)
    {
        return dir switch
        {
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            _ => throw new ArgumentException(null, nameof(dir))
        };
    }
}
