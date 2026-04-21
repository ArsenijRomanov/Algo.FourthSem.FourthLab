using HamiltonianPath.Core.Domains;

namespace HamiltonianPath.Core.Helpers;

public static class DirectionHelper
{
    public static ReadOnlySpan<Direction> All =>
    [
        Direction.Left,
        Direction.Down,
        Direction.Right,
        Direction.Up
    ];
    
    public static (int dX, int dY) GetOffset(Direction dir)
    {
        return dir switch
        {
            Direction.Right => (1, 0),
            Direction.Down  => (0, 1),
            Direction.Left  => (-1, 0),
            Direction.Up    => (0, -1),
            _ => (0, 0)
        };
    }
}
