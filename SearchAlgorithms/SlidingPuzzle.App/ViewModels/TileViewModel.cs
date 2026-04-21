using Avalonia.Media;
using SearchAlgorithms.UI.Shared.ViewModels;

namespace SlidingPuzzle.App.ViewModels;

public sealed class TileViewModel(byte index, byte value) : ViewModelBase
{
    private byte _value = value;

    public byte Index { get; } = index;
    public byte Value { get => _value; set { if (SetProperty(ref _value, value)) { RaisePropertyChanged(nameof(Label)); RaisePropertyChanged(nameof(Background)); } } }
    public string Label => Value == 0 ? string.Empty : Value.ToString();
    public IBrush Background => Value == 0
        ? new SolidColorBrush(Color.Parse("#1A1F27"))
        : new SolidColorBrush(Color.Parse("#384760"));
}
