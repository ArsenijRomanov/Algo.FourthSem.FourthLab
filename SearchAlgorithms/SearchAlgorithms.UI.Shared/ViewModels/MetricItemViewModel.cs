namespace SearchAlgorithms.UI.Shared.ViewModels;

public sealed class MetricItemViewModel(string name, string value)
{
    public string Name { get; } = name;
    public string Value { get; set; } = value;
}
