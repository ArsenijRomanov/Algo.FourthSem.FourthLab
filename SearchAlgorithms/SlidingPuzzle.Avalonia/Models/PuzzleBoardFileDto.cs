namespace SlidingPuzzle.Avalonia.Models;

public sealed class PuzzleBoardFileDto
{
    public int Size { get; set; }
    public byte[] Tiles { get; set; } = [];
}
