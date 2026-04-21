namespace SearchAlgorithms.UI.Shared.ViewModels;

public sealed class ResultRowViewModel
{
    public string Algorithm { get; init; } = string.Empty;
    public string TimeMs { get; init; } = string.Empty;
    public string MemoryKb { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
}
