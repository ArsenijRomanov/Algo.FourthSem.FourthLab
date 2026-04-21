using System.Collections.ObjectModel;
using System.Diagnostics;
using HamiltonianPath.Core;
using HamiltonianPath.Core.Abstractions;
using HamiltonianPath.Core.Domains;
using HamiltonianPath.Core.Strategies;
using SearchAlgorithms.UI.Shared.ViewModels;

namespace HamiltonianPath.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private int _width = 7;
    private int _height = 7;
    private CellViewModel? _start;
    private CellViewModel? _finish;
    private string _modeHint = "ЛКМ по клетке: сначала Start, потом Finish, затем стены. Повторный клик по Start сбрасывает обе точки.";

    public MainWindowViewModel()
    {
        BuildBoard();
        BuildBoardCommand = new RelayCommand(BuildBoard);
        SolveCommand = new RelayCommand(Solve);
        ClearPathCommand = new RelayCommand(ClearPath);
        ToggleCellCommand = new RelayCommand<CellViewModel>(ToggleCell);
    }

    public int Width { get => _width; set => SetProperty(ref _width, value); }
    public int Height { get => _height; set => SetProperty(ref _height, value); }
    public bool UseWarnsdorff { get; set; } = true;
    public bool UseConnectivity { get; set; } = true;
    public bool UseBackjumping { get; set; }
    public string ModeHint { get => _modeHint; set => SetProperty(ref _modeHint, value); }

    public ObservableCollection<CellViewModel> Cells { get; } = [];
    public ObservableCollection<ResultRowViewModel> Results { get; } = [];

    public RelayCommand BuildBoardCommand { get; }
    public RelayCommand SolveCommand { get; }
    public RelayCommand ClearPathCommand { get; }
    public RelayCommand<CellViewModel> ToggleCellCommand { get; }

    private void BuildBoard()
    {
        Cells.Clear();
        _start = null;
        _finish = null;

        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
            Cells.Add(new CellViewModel(x, y));

        RaisePropertyChanged(nameof(Width));
        RaisePropertyChanged(nameof(Height));
    }

    private void ClearPath()
    {
        foreach (var cell in Cells)
            cell.PathOrder = 0;
    }

    private void ToggleCell(CellViewModel? cell)
    {
        if (cell is null)
            return;

        if (_start is not null && cell == _start)
        {
            _start.IsStart = false;
            _start = null;

            if (_finish is not null)
            {
                _finish.IsFinish = false;
                _finish = null;
            }

            cell.IsWall = false;
            return;
        }

        if (_start is null)
        {
            cell.IsWall = false;
            cell.IsStart = true;
            _start = cell;
            return;
        }

        if (_finish is null && cell != _start)
        {
            cell.IsWall = false;
            cell.IsFinish = true;
            _finish = cell;
            return;
        }

        if (cell == _finish)
        {
            cell.IsFinish = false;
            _finish = null;
            return;
        }

        if (!cell.IsStart && !cell.IsFinish)
            cell.IsWall = !cell.IsWall;
    }

    private void Solve()
    {
        if (_start is null || _finish is null)
            return;

        ClearPath();

        var matrix = new int[Height, Width];
        foreach (var cell in Cells.Where(c => c.IsWall))
            matrix[cell.Y, cell.X] = -100;

        var board = new Board(matrix, new Point(_start.X, _start.Y), new Point(_finish.X, _finish.Y));
        var chooser = UseWarnsdorff ? (IChooseDirection)new WarnsdorffChooseDirection() : new BaseChooseDirection();
        var validator = UseConnectivity ? (ICommitValidator)new ConnectivityCommitValidator() : new BaseCommitValidator();
        var solver = new HamiltonianPathSolver(chooser, validator, UseBackjumping);

        var beforeBytes = GC.GetTotalMemory(false);
        var sw = Stopwatch.StartNew();
        var solved = solver.Solve(board);
        sw.Stop();
        var afterBytes = GC.GetTotalMemory(false);

        if (solved)
        {
            foreach (var cell in Cells)
            {
                if (board[cell.Y, cell.X] > 0)
                    cell.PathOrder = board[cell.Y, cell.X];
            }
        }

        Results.Insert(0, new ResultRowViewModel
        {
            Algorithm = $"{(UseWarnsdorff ? "Warnsdorff" : "Base")} + {(UseConnectivity ? "Connectivity" : "Base validator")}",
            Details = solved ? "Решение найдено" : "Решения нет",
            TimeMs = $"Time: {sw.Elapsed.TotalMilliseconds:F2} ms",
            MemoryKb = $"Memory Δ: {(afterBytes - beforeBytes) / 1024.0:F1} KB"
        });
    }
}
