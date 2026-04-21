using Avalonia.Media;
using SearchAlgorithms.UI.Shared.ViewModels;

namespace HamiltonianPath.App.ViewModels;

public sealed class CellViewModel(int x, int y) : ViewModelBase
{
    private bool _isWall;
    private bool _isStart;
    private bool _isFinish;
    private int _pathOrder;

    public int X { get; } = x;
    public int Y { get; } = y;

    public bool IsWall { get => _isWall; set { if (SetProperty(ref _isWall, value)) Refresh(); } }
    public bool IsStart { get => _isStart; set { if (SetProperty(ref _isStart, value)) Refresh(); } }
    public bool IsFinish { get => _isFinish; set { if (SetProperty(ref _isFinish, value)) Refresh(); } }
    public int PathOrder { get => _pathOrder; set { if (SetProperty(ref _pathOrder, value)) Refresh(); } }

    public IBrush Background =>
        IsStart ? new SolidColorBrush(Color.Parse("#2FAA7A")) :
        IsFinish ? new SolidColorBrush(Color.Parse("#D64867")) :
        IsWall ? new SolidColorBrush(Color.Parse("#4A5364")) :
        PathOrder > 0 ? new SolidColorBrush(Color.Parse("#4A6BCA")) :
        new SolidColorBrush(Color.Parse("#222A35"));

    public string Label => IsStart ? "S" : IsFinish ? "F" : (PathOrder > 0 ? PathOrder.ToString() : string.Empty);

    private void Refresh()
    {
        RaisePropertyChanged(nameof(Background));
        RaisePropertyChanged(nameof(Label));
    }
}
