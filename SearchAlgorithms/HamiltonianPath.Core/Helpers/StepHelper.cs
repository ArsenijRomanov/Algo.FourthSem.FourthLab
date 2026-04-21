using HamiltonianPath.Core.Domains;
using HamiltonianPath.Core.Enums;

namespace HamiltonianPath.Core.Helpers;

public static class StepHelper
{
    public static ReadOnlySpan<DirectionFlag> All =>
    [
        DirectionFlag.Left,
        DirectionFlag.Down,
        DirectionFlag.Right,
        DirectionFlag.Up
    ];
    
    public static (int dX, int dY) GetOffset(DirectionFlag dir)
    {
        return dir switch
        {
            DirectionFlag.Right => (1, 0),
            DirectionFlag.Down  => (0, 1),
            DirectionFlag.Left  => (-1, 0),
            DirectionFlag.Up    => (0, -1),
            _ => (0, 0)
        };
    }
}
