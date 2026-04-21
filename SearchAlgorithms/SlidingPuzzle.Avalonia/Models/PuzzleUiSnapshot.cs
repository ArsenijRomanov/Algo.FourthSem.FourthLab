using SearchAlgorithms.UI.Shared.Models;

namespace SlidingPuzzle.Avalonia.Models;

public sealed class PuzzleUiSnapshot
{
    public required byte[] Tiles { get; init; }
    public required PlaybackStatus PlaybackStatus { get; init; }
    public required int CurrentStepIndex { get; init; }
    public required bool HasSolution { get; init; }
    public required bool IsEditMode { get; init; }
}
