namespace HamiltonianPath.Core.Enums;

[Flags]
public enum DirectionFlag: byte
{
    None = 0,
    Right = 1,
    Down = 2,
    Left = 4,
    Up = 8
}
