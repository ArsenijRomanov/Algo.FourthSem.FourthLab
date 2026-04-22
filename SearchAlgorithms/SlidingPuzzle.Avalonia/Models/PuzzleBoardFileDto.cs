namespace SlidingPuzzle.Avalonia.Models;

public sealed class PuzzleBoardFileDto
{
    public int Size { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public byte[] Tiles { get; set; } = [];
}
