using Avalonia.Media;
using SearchAlgorithms.UI.Shared.Mvvm;

namespace HamiltonianPath.Avalonia.ViewModels;

public sealed class HamiltonianBoardCellViewModel : ObservableObject
{
    private bool _isWall;
    private bool _isStart;
    private bool _isFinish;
    private int _pathIndex;

    public HamiltonianBoardCellViewModel(int row, int column)
    {
        Row = row;
        Column = column;
        RefreshVisuals();
    }

    public int Row { get; }
    public int Column { get; }
    public int Index => Row * 1000 + Column;

    public bool IsWall
    {
        get => _isWall;
        set
        {
            if (SetProperty(ref _isWall, value))
                RefreshVisuals();
        }
    }

    public bool IsStart
    {
        get => _isStart;
        set
        {
            if (SetProperty(ref _isStart, value))
                RefreshVisuals();
        }
    }

    public bool IsFinish
    {
        get => _isFinish;
        set
        {
            if (SetProperty(ref _isFinish, value))
                RefreshVisuals();
        }
    }

    public int PathIndex
    {
        get => _pathIndex;
        set
        {
            if (SetProperty(ref _pathIndex, value))
                RefreshVisuals();
        }
    }

    public string DisplayText { get; private set; } = string.Empty;
    public IBrush BackgroundBrush { get; private set; } = Brushes.Transparent;
    public IBrush BorderBrush { get; private set; } = new SolidColorBrush(Color.Parse("#334155"));
    public IBrush ForegroundBrush { get; private set; } = Brushes.White;

    public void ResetPath() => PathIndex = 0;

    private void RefreshVisuals()
    {
        if (IsStart)
        {
            BackgroundBrush = new SolidColorBrush(Color.Parse("#1E5D46"));
            BorderBrush = new SolidColorBrush(Color.Parse("#39C98A"));
            ForegroundBrush = Brushes.White;
            DisplayText = "S";
        }
        else if (IsFinish)
        {
            BackgroundBrush = new SolidColorBrush(Color.Parse("#5A2332"));
            BorderBrush = new SolidColorBrush(Color.Parse("#FF6D7A"));
            ForegroundBrush = Brushes.White;
            DisplayText = "F";
        }
        else if (IsWall)
        {
            BackgroundBrush = new SolidColorBrush(Color.Parse("#2A2E38"));
            BorderBrush = new SolidColorBrush(Color.Parse("#3A4353"));
            ForegroundBrush = Brushes.Transparent;
            DisplayText = string.Empty;
        }
        else if (PathIndex > 0)
        {
            BackgroundBrush = new SolidColorBrush(Color.Parse("#2E2A57"));
            BorderBrush = new SolidColorBrush(Color.Parse("#8E86FF"));
            ForegroundBrush = Brushes.White;
            DisplayText = PathIndex.ToString();
        }
        else
        {
            BackgroundBrush = new SolidColorBrush(Color.Parse("#1B1F29"));
            BorderBrush = new SolidColorBrush(Color.Parse("#334155"));
            ForegroundBrush = Brushes.White;
            DisplayText = string.Empty;
        }

        OnPropertiesChanged(nameof(DisplayText), nameof(BackgroundBrush), nameof(BorderBrush), nameof(ForegroundBrush));
    }
}
