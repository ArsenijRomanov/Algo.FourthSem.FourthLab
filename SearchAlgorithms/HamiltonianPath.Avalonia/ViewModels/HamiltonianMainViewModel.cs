using System.Collections.ObjectModel;
using HamiltonianPath.Avalonia.Models;
using HamiltonianPath.Core;
using HamiltonianPath.Core.Abstractions;
using HamiltonianPath.Core.Domains;
using HamiltonianPath.Core.Strategies;
using SearchAlgorithms.UI.Shared.Helpers;
using SearchAlgorithms.UI.Shared.Models;
using SearchAlgorithms.UI.Shared.Mvvm;
using SearchAlgorithms.UI.Shared.Services;

namespace HamiltonianPath.Avalonia.ViewModels;

public sealed class HamiltonianMainViewModel : ObservableObject
{
    private readonly BenchmarkService _benchmarkService;
    private HamiltonianTool _selectedTool = HamiltonianTool.Wall;
    private int _boardWidth = 7;
    private int _boardHeight = 7;
    private bool _useWarnsdorff;
    private bool _useConnectivity;
    private bool _useBackjumping;
    private int? _startRow;
    private int? _startColumn;
    private int? _finishRow;
    private int? _finishColumn;
    private string _statusText = "Настройте поле и запустите решатель.";
    private bool _isBusy;

    public HamiltonianMainViewModel(BenchmarkService benchmarkService)
    {
        _benchmarkService = benchmarkService;

        Cells = [];
        Results = [];

        ResizeBoardCommand = new RelayCommand(ResizeBoard);
        ClearWallsCommand = new RelayCommand(ClearWalls);
        ClearEndpointsCommand = new RelayCommand(ClearEndpoints);
        ClearAllCommand = new RelayCommand(ClearAll);
        RunCurrentCommand = new AsyncRelayCommand(RunCurrentAsync, () => !IsBusy);
        RunBaselineAndCurrentCommand = new AsyncRelayCommand(RunBaselineAndCurrentAsync, () => !IsBusy);

        RebuildCells();
    }

    public ObservableCollection<HamiltonianBoardCellViewModel> Cells { get; }
    public ObservableCollection<AlgorithmRunRecord> Results { get; }

    public RelayCommand ResizeBoardCommand { get; }
    public RelayCommand ClearWallsCommand { get; }
    public RelayCommand ClearEndpointsCommand { get; }
    public RelayCommand ClearAllCommand { get; }
    public AsyncRelayCommand RunCurrentCommand { get; }
    public AsyncRelayCommand RunBaselineAndCurrentCommand { get; }

    public int BoardWidth
    {
        get => _boardWidth;
        set => SetProperty(ref _boardWidth, Math.Clamp(value, 2, 20));
    }

    public int BoardHeight
    {
        get => _boardHeight;
        set => SetProperty(ref _boardHeight, Math.Clamp(value, 2, 20));
    }

    public bool UseWarnsdorff
    {
        get => _useWarnsdorff;
        set => SetProperty(ref _useWarnsdorff, value);
    }

    public bool UseConnectivity
    {
        get => _useConnectivity;
        set => SetProperty(ref _useConnectivity, value);
    }

    public bool UseBackjumping
    {
        get => _useBackjumping;
        set => SetProperty(ref _useBackjumping, value);
    }

    public HamiltonianTool SelectedTool
    {
        get => _selectedTool;
        set => SetProperty(ref _selectedTool, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RunCurrentCommand.NotifyCanExecuteChanged();
                RunBaselineAndCurrentCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public void ResizeBoard()
    {
        RebuildCells();
        StatusText = $"Размер поля изменён: {BoardHeight} × {BoardWidth}.";
    }

    public void SetTool(HamiltonianTool tool) => SelectedTool = tool;

    public void ApplyToolToCell(int row, int column)
    {
        if (row < 0 || column < 0 || row >= BoardHeight || column >= BoardWidth)
            return;

        var cell = GetCell(row, column);

        switch (SelectedTool)
        {
            case HamiltonianTool.Wall:
                if (!cell.IsStart && !cell.IsFinish)
                {
                    cell.IsWall = true;
                    cell.PathIndex = 0;
                }
                break;

            case HamiltonianTool.Erase:
                if (cell.IsStart)
                {
                    ClearEndpoints();
                    return;
                }

                if (cell.IsFinish)
                {
                    ClearFinishOnly();
                    return;
                }

                cell.IsWall = false;
                cell.PathIndex = 0;
                break;

            case HamiltonianTool.Start:
                if (cell.IsStart)
                {
                    ClearEndpoints();
                    return;
                }

                ClearStartOnly();
                cell.IsWall = false;
                cell.PathIndex = 0;
                cell.IsStart = true;
                _startRow = row;
                _startColumn = column;
                break;

            case HamiltonianTool.Finish:
                if (cell.IsFinish)
                {
                    ClearFinishOnly();
                    return;
                }

                ClearFinishOnly();
                cell.IsWall = false;
                cell.PathIndex = 0;
                cell.IsFinish = true;
                _finishRow = row;
                _finishColumn = column;
                break;
        }
    }

    public void PaintWalls(int row, int column, bool erase)
    {
        if (row < 0 || column < 0 || row >= BoardHeight || column >= BoardWidth)
            return;

        var cell = GetCell(row, column);

        if (cell.IsStart || cell.IsFinish)
            return;

        cell.IsWall = !erase;
        cell.PathIndex = 0;
    }

    public void ClearWalls()
    {
        foreach (var cell in Cells)
        {
            cell.IsWall = false;
            cell.PathIndex = 0;
        }

        StatusText = "Стены очищены.";
    }

    public void ClearEndpoints()
    {
        ClearStartOnly();
        ClearFinishOnly();
        StatusText = "Старт и финиш очищены.";
    }

    public void ClearAll()
    {
        RebuildCells();
        Results.Clear();
        StatusText = "Поле и результаты очищены.";
    }

    private Task RunCurrentAsync()
    {
        IsBusy = true;
        try
        {
            var result = SolveCurrentConfiguration();
            Results.Insert(0, result);
            StatusText = result.Note ?? result.StatusText;
        }
        finally
        {
            IsBusy = false;
        }

        return Task.CompletedTask;
    }

    private Task RunBaselineAndCurrentAsync()
    {
        IsBusy = true;
        try
        {
            var baseline = SolveConfiguration(false, false, false);
            Results.Insert(0, baseline);

            var current = SolveCurrentConfiguration();
            if (current.Title != baseline.Title)
                Results.Insert(0, current);

            StatusText = "Базовый и текущий прогоны завершены.";
        }
        finally
        {
            IsBusy = false;
        }

        return Task.CompletedTask;
    }

    private AlgorithmRunRecord SolveCurrentConfiguration()
        => SolveConfiguration(UseWarnsdorff, UseConnectivity, UseBackjumping);

    private AlgorithmRunRecord SolveConfiguration(bool warnsdorff, bool connectivity, bool backjumping)
    {
        ClearSolutionPath();

        if (_startRow is null || _startColumn is null || _finishRow is null || _finishColumn is null)
        {
            return new AlgorithmRunRecord
            {
                Title = BuildAlgorithmTitle(warnsdorff, connectivity, backjumping),
                IsSuccess = false,
                StatusText = "Не задан старт/финиш",
                Elapsed = TimeSpan.Zero,
                ManagedMemoryDeltaBytes = 0,
                WorkingSetDeltaBytes = 0,
                Steps = 0,
                Note = "Перед запуском нужно указать старт и финиш."
            };
        }

        var matrix = new int[BoardHeight, BoardWidth];
        foreach (var cell in Cells)
            matrix[cell.Row, cell.Column] = cell.IsWall ? -100 : 0;

        var start = new Point(_startColumn.Value, _startRow.Value);
        var finish = new Point(_finishColumn.Value, _finishRow.Value);

        var board = new Board(matrix, start, finish);

        if (!HamiltonianPathSolver.CanHaveHamiltonianPath(board))
        {
            return new AlgorithmRunRecord
            {
                Title = BuildAlgorithmTitle(warnsdorff, connectivity, backjumping),
                IsSuccess = false,
                StatusText = "Отклонено пред-проверкой",
                Elapsed = TimeSpan.Zero,
                ManagedMemoryDeltaBytes = 0,
                WorkingSetDeltaBytes = 0,
                Steps = 0,
                Note = "При текущем старте, финише и стенах гамильтонов путь невозможен."
            };
        }

        IChooseDirection chooseDirection = warnsdorff
            ? new WarnsdorffChooseDirection()
            : new BaseChooseDirection();

        ICommitValidator commitValidator = connectivity
            ? new ConnectivityCommitValidator()
            : new BaseCommitValidator();

        var solver = new HamiltonianPathSolver(chooseDirection, commitValidator, backjumping);
        var benchmark = _benchmarkService.Run(() => solver.Solve(board));

        if (benchmark.Result)
            ApplySolution(board);

        var solvedSteps = Cells.Where(static c => c.PathIndex > 0).Count();

        return new AlgorithmRunRecord
        {
            Title = BuildAlgorithmTitle(warnsdorff, connectivity, backjumping),
            IsSuccess = benchmark.Result,
            StatusText = benchmark.Result ? "Решено" : "Путь не найден",
            Elapsed = benchmark.Elapsed,
            ManagedMemoryDeltaBytes = Math.Max(0, benchmark.ManagedMemoryDeltaBytes),
            WorkingSetDeltaBytes = Math.Max(0, benchmark.WorkingSetDeltaBytes),
            Steps = solvedSteps,
            Note = benchmark.Result
                ? $"Длина пути: {solvedSteps}. Память (managed): {FormatHelper.FormatBytes(Math.Max(0, benchmark.ManagedMemoryDeltaBytes))}."
                : "Решатель завершил работу без найденного гамильтонова пути."
        };
    }

    private void ApplySolution(Board board)
    {
        foreach (var cell in Cells)
        {
            if (!cell.IsWall && !cell.IsStart && !cell.IsFinish)
                cell.PathIndex = board[cell.Row, cell.Column];
        }
    }

    private void ClearSolutionPath()
    {
        foreach (var cell in Cells)
        {
            if (!cell.IsWall && !cell.IsStart && !cell.IsFinish)
                cell.PathIndex = 0;
        }
    }

    private void RebuildCells()
    {
        Cells.Clear();
        _startRow = _startColumn = _finishRow = _finishColumn = null;

        for (var row = 0; row < BoardHeight; row++)
        {
            for (var column = 0; column < BoardWidth; column++)
                Cells.Add(new HamiltonianBoardCellViewModel(row, column));
        }

        ClearSolutionPath();
        OnPropertyChanged(nameof(Cells));
    }

    private HamiltonianBoardCellViewModel GetCell(int row, int column)
        => Cells[row * BoardWidth + column];

    private void ClearStartOnly()
    {
        if (_startRow is null || _startColumn is null)
            return;

        var cell = GetCell(_startRow.Value, _startColumn.Value);
        cell.IsStart = false;
        _startRow = null;
        _startColumn = null;
    }

    private void ClearFinishOnly()
    {
        if (_finishRow is null || _finishColumn is null)
            return;

        var cell = GetCell(_finishRow.Value, _finishColumn.Value);
        cell.IsFinish = false;
        _finishRow = null;
        _finishColumn = null;
    }

    private static string BuildAlgorithmTitle(bool warnsdorff, bool connectivity, bool backjumping)
    {
        var parts = new List<string> { "База" };
        if (warnsdorff) parts.Add("Варнсдорф");
        if (connectivity) parts.Add("Связность");
        if (backjumping) parts.Add("Backjumping");
        return string.Join(" + ", parts);
    }
}
