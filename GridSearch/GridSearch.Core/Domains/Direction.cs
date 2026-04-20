namespace GridSearch.Core.Domains;

[Flags]
public enum Direction: byte
{
    None = 0,
    Right = 1,
    Down = 2,
    Left = 4,
    Up = 8
}
