using global::Avalonia;
using global::Avalonia.Media;
using SearchAlgorithms.UI.Shared.Mvvm;

namespace SlidingPuzzle.Avalonia.ViewModels;

public sealed class PuzzleTileViewModel : ObservableObject
{
    private byte _value;
    private bool _isDragSource;
    private bool _isDragTarget;

    public PuzzleTileViewModel(int index, byte value)
    {
        Index = index;
        Value = value;
    }

    public int Index { get; set; }

    public byte Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value))
                RefreshVisuals();
        }
    }

    public bool IsDragSource
    {
        get => _isDragSource;
        set
        {
            if (SetProperty(ref _isDragSource, value))
                OnPropertiesChanged(nameof(ScaleFactor), nameof(BackgroundBrush), nameof(BorderBrush), nameof(BorderThicknessValue));
        }
    }

    public bool IsDragTarget
    {
        get => _isDragTarget;
        set
        {
            if (SetProperty(ref _isDragTarget, value))
                OnPropertiesChanged(nameof(BorderBrush), nameof(BorderThicknessValue));
        }
    }

    public bool IsBlank => Value == 0;
    public string Text => IsBlank ? string.Empty : Value.ToString();
    public double ScaleFactor => IsDragSource ? 1.05 : 1.0;

    public IBrush BackgroundBrush => IsDragSource
        ? new SolidColorBrush(Color.Parse("#232A37"))
        : IsBlank
            ? new SolidColorBrush(Color.Parse("#222733"))
            : new SolidColorBrush(Color.Parse("#2A3140"));

    public IBrush BorderBrush => IsDragTarget
        ? new SolidColorBrush(Color.Parse("#67A7FF"))
        : new SolidColorBrush(Color.Parse("#3B4558"));

    public Thickness BorderThicknessValue => new(IsDragTarget ? 2 : 1);

    public void RefreshVisuals()
        => OnPropertiesChanged(nameof(IsBlank), nameof(Text), nameof(BackgroundBrush), nameof(BorderBrush), nameof(BorderThicknessValue));
}
