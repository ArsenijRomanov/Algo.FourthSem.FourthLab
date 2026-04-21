using System.Numerics;

namespace HamiltonianPath.Core.Domains;

public struct PathState(Point point, Direction dirsMask = Direction.None)
{
    public Point Point { get; } = point;
    public Direction DirsMask { get; set; } = dirsMask;

    public byte AvailableDirectionsCount => (byte)BitOperations.PopCount((uint)DirsMask);

    public bool CanMove(Direction dir)
        => (DirsMask & dir) != 0;

    public void RemoveDirection(Direction dir)
        => DirsMask &= ~dir;
}
