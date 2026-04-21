using System.Numerics;
using HamiltonianPath.Core.Enums;

namespace HamiltonianPath.Core.Domains;

public struct PathState(Point point, DirectionFlag dirsMask = DirectionFlag.None)
{
    public Point Point { get; } = point;
    public DirectionFlag DirsMask { get; set; } = dirsMask;

    public byte AvailableDirectionsCount => (byte)BitOperations.PopCount((uint)DirsMask);

    public bool CanMove(DirectionFlag dir)
        => (DirsMask & dir) != 0;

    public void RemoveDirection(DirectionFlag dir)
        => DirsMask &= ~dir;
}
