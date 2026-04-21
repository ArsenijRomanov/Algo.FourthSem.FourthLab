using SearchAlgorithms.UI.Shared.Mvvm;

namespace SlidingPuzzle.Avalonia.ViewModels;

public sealed class MoveChipViewModel : ObservableObject
{
    private bool _isActive;

    public required string Text { get; init; }
    public int StepIndex { get; init; }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
}
