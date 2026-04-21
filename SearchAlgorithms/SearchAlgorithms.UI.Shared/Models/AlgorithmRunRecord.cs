using SearchAlgorithms.UI.Shared.Mvvm;

namespace SearchAlgorithms.UI.Shared.Models;

public sealed class AlgorithmRunRecord : ObservableObject
{
    private bool _isSelected;

    public required string Title { get; init; }
    public required bool IsSuccess { get; init; }
    public required string StatusText { get; init; }
    public required TimeSpan Elapsed { get; init; }
    public required long ManagedMemoryDeltaBytes { get; init; }
    public required long WorkingSetDeltaBytes { get; init; }
    public required int Steps { get; init; }
    public string? Note { get; init; }
    public DateTimeOffset ExecutedAt { get; init; } = DateTimeOffset.Now;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
