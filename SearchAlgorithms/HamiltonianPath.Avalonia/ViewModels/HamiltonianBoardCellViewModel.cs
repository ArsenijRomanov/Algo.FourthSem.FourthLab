using Avalonia.Media;
using SearchAlgorithms.UI.Shared.Mvvm;

namespace HamiltonianPath.Avalonia.ViewModels;

public sealed class HamiltonianBoardCellViewModel : ObservableObject
{
    private bool _isWall;
    private bool _isStart;
    private bool _isFinish;
    private int _pathIndex;
    private bool _linkTop;
    private bool _linkRight;
    private bool _linkBottom;
    private bool _linkLeft;

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
    public bool LinkTop
    {
        get => _linkTop;
        private set => SetProperty(ref _linkTop, value);
    }
    public bool LinkRight
    {
        get => _linkRight;
        private set => SetProperty(ref _linkRight, value);
    }
    public bool LinkBottom
    {
        get => _linkBottom;
        private set => SetProperty(ref _linkBottom, value);
    }
    public bool LinkLeft
    {
        get => _linkLeft;
        private set => SetProperty(ref _linkLeft, value);
    }
    public bool HasPathSegment => PathIndex > 0 || LinkTop || LinkRight || LinkBottom || LinkLeft;

    public void ResetPath() => PathIndex = 0;

    public void SetLinks(bool top, bool right, bool bottom, bool left)
    {
        LinkTop = top;
        LinkRight = right;
        LinkBottom = bottom;
        LinkLeft = left;
        OnPropertyChanged(nameof(HasPathSegment));
    }

    private void RefreshVisuals()
    {
        if (IsStart)
        {
            BackgroundBrush = new SolidColorBrush(Color.Parse("#1E5D46"));
            BorderBrush = new SolidColorBrush(Color.Parse("#39C98A"));
            ForegroundBrush = Brushes.White;
            DisplayText = string.Empty;
        }
        else if (IsFinish)
        {
            BackgroundBrush = new SolidColorBrush(Color.Parse("#5A2332"));
            BorderBrush = new SolidColorBrush(Color.Parse("#FF6D7A"));
            ForegroundBrush = Brushes.White;
            DisplayText = string.Empty;
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
            DisplayText = string.Empty;
        }
        else
        {
            BackgroundBrush = new SolidColorBrush(Color.Parse("#1B1F29"));
            BorderBrush = new SolidColorBrush(Color.Parse("#334155"));
            ForegroundBrush = Brushes.White;
            DisplayText = string.Empty;
        }

        OnPropertiesChanged(nameof(DisplayText), nameof(BackgroundBrush), nameof(BorderBrush), nameof(ForegroundBrush), nameof(HasPathSegment));
    }
}
