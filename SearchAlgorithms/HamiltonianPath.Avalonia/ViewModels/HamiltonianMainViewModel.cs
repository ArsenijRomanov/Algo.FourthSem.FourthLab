using System.Collections.ObjectModel;
using System.Diagnostics;
using HamiltonianPath.Avalonia.Models;
using HamiltonianPath.Core;
using HamiltonianPath.Core.Abstractions;
using HamiltonianPath.Core.Domains;
using HamiltonianPath.Core.Strategies;
using SearchAlgorithms.UI.Shared.Helpers;
using SearchAlgorithms.UI.Shared.Models;
using SearchAlgorithms.UI.Shared.Mvvm;

namespace HamiltonianPath.Avalonia.ViewModels;

public sealed class HamiltonianMainViewModel : ObservableObject
{
    private HamiltonianTool _selectedTool = HamiltonianTool.Wall;
    private int _boardWidth = 7;
    private int _boardHeight = 7;
    private int _pendingBoardWidth = 7;
    private int _pendingBoardHeight = 7;
    private bool _useWarnsdorff;
    private bool _useConnectivity;
    private bool _useBackjumping;
    private int? _startRow;
    private int? _startColumn;
    private int? _finishRow;
    private int? _finishColumn;
    private string _statusText = "";
    private bool _isBusy;
    private CancellationTokenSource? _runCancellationTokenSource;

    public HamiltonianMainViewModel()
    {
        Cells = [];
        Results = [];

        ResizeBoardCommand = new RelayCommand(ResizeBoard);
        ClearWallsCommand = new RelayCommand(ClearWalls);
        ClearEndpointsCommand = new RelayCommand(ClearEndpoints);
        ClearAllCommand = new RelayCommand(ClearAll);
        ClearResultsCommand = new RelayCommand(ClearResults);
        RunCurrentCommand = new AsyncRelayCommand(RunCurrentAsync, () => !IsBusy);
        RunBaselineAndCurrentCommand = new AsyncRelayCommand(RunBaselineAndCurrentAsync, () => !IsBusy);
        CancelRunCommand = new RelayCommand(CancelRun, () => IsBusy);

        RebuildCells();
    }

    public ObservableCollection<HamiltonianBoardCellViewModel> Cells { get; }
    public ObservableCollection<AlgorithmRunRecord> Results { get; }

    public RelayCommand ResizeBoardCommand { get; }
    public RelayCommand ClearWallsCommand { get; }
    public RelayCommand ClearEndpointsCommand { get; }
    public RelayCommand ClearAllCommand { get; }
    public RelayCommand ClearResultsCommand { get; }
    public AsyncRelayCommand RunCurrentCommand { get; }
    public AsyncRelayCommand RunBaselineAndCurrentCommand { get; }
    public RelayCommand CancelRunCommand { get; }

    public int BoardWidth
    {
        get => _boardWidth;
        private set => SetProperty(ref _boardWidth, Math.Clamp(value, 2, 20));
    }

    public int BoardHeight
    {
        get => _boardHeight;
        private set => SetProperty(ref _boardHeight, Math.Clamp(value, 2, 20));
    }

    public int PendingBoardWidth
    {
        get => _pendingBoardWidth;
        set => SetProperty(ref _pendingBoardWidth, Math.Clamp(value, 2, 20));
    }

    public int PendingBoardHeight
    {
        get => _pendingBoardHeight;
        set => SetProperty(ref _pendingBoardHeight, Math.Clamp(value, 2, 20));
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
                CancelRunCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(IsInteractionEnabled));
            }
        }
    }

    public bool IsInteractionEnabled => !IsBusy;

    public void ResizeBoard()
    {
        BoardWidth = PendingBoardWidth;
        BoardHeight = PendingBoardHeight;
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

        RefreshPathLinks();
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
        RefreshPathLinks();
    }

    public void ClearWalls()
    {
        foreach (var cell in Cells)
        {
            cell.IsWall = false;
            cell.PathIndex = 0;
        }

        StatusText = "Стены очищены.";
        RefreshPathLinks();
    }

    public void ClearEndpoints()
    {
        ClearStartOnly();
        ClearFinishOnly();
        StatusText = "";
        RefreshPathLinks();
    }

    public void ClearAll()
    {
        RebuildCells();
        Results.Clear();
        StatusText = "Поле и результаты очищены.";
    }

    public void ClearResults()
    {
        Results.Clear();
        StatusText = "Результаты очищены.";
    }

    private async Task RunCurrentAsync()
    {
        var pathSnapshot = CapturePathSnapshot();
        _runCancellationTokenSource?.Dispose();
        _runCancellationTokenSource = new CancellationTokenSource();
        IsBusy = true;
        try
        {
            AlgorithmRunRecord result;
            try
            {
                result = await SolveCurrentConfigurationAsync(_runCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                RestorePathSnapshot(pathSnapshot);
                StatusText = "Выполнение прервано пользователем.";
                return;
            }
            catch (Exception ex)
            {
                result = CreateCrashResult(ex, UseWarnsdorff, UseConnectivity, UseBackjumping);
            }

            Results.Insert(0, result);
            StatusText = result.Note ?? result.StatusText;
        }
        finally
        {
            IsBusy = false;
            _runCancellationTokenSource?.Dispose();
            _runCancellationTokenSource = null;
        }
    }

    private async Task RunBaselineAndCurrentAsync()
    {
        var pathSnapshot = CapturePathSnapshot();
        _runCancellationTokenSource?.Dispose();
        _runCancellationTokenSource = new CancellationTokenSource();
        IsBusy = true;
        try
        {
            AlgorithmRunRecord baseline;
            try
            {
                baseline = await SolveConfigurationAsync(false, false, false, _runCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                RestorePathSnapshot(pathSnapshot);
                StatusText = "Выполнение прервано пользователем.";
                return;
            }
            catch (Exception ex)
            {
                baseline = CreateCrashResult(ex, false, false, false);
            }

            Results.Insert(0, baseline);

            AlgorithmRunRecord current;
            try
            {
                current = await SolveCurrentConfigurationAsync(_runCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                RestorePathSnapshot(pathSnapshot);
                StatusText = "Выполнение прервано пользователем.";
                return;
            }
            catch (Exception ex)
            {
                current = CreateCrashResult(ex, UseWarnsdorff, UseConnectivity, UseBackjumping);
            }

            if (current.Title != baseline.Title)
                Results.Insert(0, current);

            StatusText = "Базовый и текущий прогоны завершены.";
        }
        finally
        {
            IsBusy = false;
            _runCancellationTokenSource?.Dispose();
            _runCancellationTokenSource = null;
        }
    }

    private Task<AlgorithmRunRecord> SolveCurrentConfigurationAsync(CancellationToken cancellationToken)
        => SolveConfigurationAsync(UseWarnsdorff, UseConnectivity, UseBackjumping, cancellationToken);

    private async Task<AlgorithmRunRecord> SolveConfigurationAsync(bool warnsdorff, bool connectivity, bool backjumping, CancellationToken cancellationToken)
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
        var computation = await Task.Run(
            () => ComputeSolve(warnsdorff, connectivity, backjumping, matrix, start, finish, cancellationToken),
            cancellationToken);

        if (computation.SolvedMatrix is not null)
            ApplySolution(computation.SolvedMatrix);

        return computation.Record;
    }

    private static SolveComputation ComputeSolve(bool warnsdorff, bool connectivity, bool backjumping, int[,] matrix, Point start, Point finish, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var board = new Board((int[,])matrix.Clone(), start, finish);

        if (!HamiltonianPathSolver.CanHaveHamiltonianPath(board))
        {
            return new SolveComputation(new AlgorithmRunRecord
            {
                Title = BuildAlgorithmTitle(warnsdorff, connectivity, backjumping),
                IsSuccess = false,
                StatusText = "Отклонено",
                Elapsed = TimeSpan.Zero,
                ManagedMemoryDeltaBytes = 0,
                WorkingSetDeltaBytes = 0,
                Steps = 0,
                Note = "При текущем старте, финише и стенах гамильтонов путь невозможен."
            }, null);
        }

        IChooseDirection chooseDirection = warnsdorff
            ? new WarnsdorffChooseDirection()
            : new BaseChooseDirection();

        ICommitValidator commitValidator = connectivity
            ? new ConnectivityCommitValidator()
            : new BaseCommitValidator();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var managedBefore = GC.GetTotalMemory(true);
        var stopwatch = Stopwatch.StartNew();
        var solver = new HamiltonianPathSolver(chooseDirection, commitValidator, backjumping);
        var isSolved = solver.Solve(board, cancellationToken);
        stopwatch.Stop();
        var managedAfter = GC.GetTotalMemory(true);
        var managedDelta = Math.Max(0, managedAfter - managedBefore);

        var solvedMatrix = isSolved ? ExtractPathMatrix(board, matrix.GetLength(0), matrix.GetLength(1)) : null;
        var solvedSteps = isSolved && solvedMatrix is not null
            ? CountSolvedSteps(solvedMatrix)
            : 0;

        return new SolveComputation(new AlgorithmRunRecord
        {
            Title = BuildAlgorithmTitle(warnsdorff, connectivity, backjumping),
            IsSuccess = isSolved,
            StatusText = isSolved ? "Решено" : "Гамильтонова пути не существует",
            Elapsed = stopwatch.Elapsed,
            ManagedMemoryDeltaBytes = managedDelta,
            WorkingSetDeltaBytes = 0,
            Steps = solvedSteps,
            Note = ""
        }, solvedMatrix);
    }

    private static int[,] ExtractPathMatrix(Board board, int height, int width)
    {
        var solved = new int[height, width];
        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
                solved[row, col] = board[row, col];
        }

        return solved;
    }

    private static int CountSolvedSteps(int[,] solvedMatrix)
    {
        var steps = 0;
        for (var row = 0; row < solvedMatrix.GetLength(0); row++)
        {
            for (var col = 0; col < solvedMatrix.GetLength(1); col++)
            {
                if (solvedMatrix[row, col] > 1)
                    steps++;
            }
        }

        return steps;
    }

    private void ApplySolution(int[,] solvedMatrix)
    {
        foreach (var cell in Cells)
        {
            if (!cell.IsWall && !cell.IsStart && !cell.IsFinish)
                cell.PathIndex = solvedMatrix[cell.Row, cell.Column];
        }

        RefreshPathLinks();
    }

    private sealed record SolveComputation(AlgorithmRunRecord Record, int[,]? SolvedMatrix);

    private void ClearSolutionPath()
    {
        foreach (var cell in Cells)
        {
            if (!cell.IsWall && !cell.IsStart && !cell.IsFinish)
                cell.PathIndex = 0;
        }

        RefreshPathLinks();
    }

    private Dictionary<(int Row, int Column), int> CapturePathSnapshot()
        => Cells.ToDictionary(static cell => (cell.Row, cell.Column), static cell => cell.PathIndex);

    private void RestorePathSnapshot(Dictionary<(int Row, int Column), int> snapshot)
    {
        foreach (var cell in Cells)
        {
            if (snapshot.TryGetValue((cell.Row, cell.Column), out var pathIndex))
                cell.PathIndex = pathIndex;
        }

        RefreshPathLinks();
    }

    private void CancelRun()
        => _runCancellationTokenSource?.Cancel();

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
        RefreshPathLinks();
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
        RefreshPathLinks();
    }

    private void ClearFinishOnly()
    {
        if (_finishRow is null || _finishColumn is null)
            return;

        var cell = GetCell(_finishRow.Value, _finishColumn.Value);
        cell.IsFinish = false;
        _finishRow = null;
        _finishColumn = null;
        RefreshPathLinks();
    }

    private static string BuildAlgorithmTitle(bool warnsdorff, bool connectivity, bool backjumping)
    {
        var parts = new List<string> { "База" };
        if (warnsdorff) parts.Add("Варнсдорф");
        if (connectivity) parts.Add("Связность");
        if (backjumping) parts.Add("Backjumping");
        return string.Join(" + ", parts);
    }

    private static AlgorithmRunRecord CreateCrashResult(Exception ex, bool warnsdorff, bool connectivity, bool backjumping) => new()
    {
        Title = BuildAlgorithmTitle(warnsdorff, connectivity, backjumping),
        IsSuccess = false,
        StatusText = "Ошибка выполнения",
        Elapsed = TimeSpan.Zero,
        ManagedMemoryDeltaBytes = 0,
        WorkingSetDeltaBytes = 0,
        Steps = 0,
        Note = $"Исключение: {ex.GetType().Name}. {ex.Message}"
    };

    private void RefreshPathLinks()
    {
        foreach (var cell in Cells)
            cell.SetLinks(false, false, false, false);

        var indexedCells = Cells.Where(static c => c.PathIndex > 0).ToList();
        if (indexedCells.Count == 0)
            return;

        var byIndex = indexedCells.ToDictionary(static c => c.PathIndex);
        var minCell = byIndex[byIndex.Keys.Min()];
        var maxCell = byIndex[byIndex.Keys.Max()];

        foreach (var cell in indexedCells)
        {
            var top = TryGetPathIndex(cell.Row - 1, cell.Column, out var topIndex) && Math.Abs(topIndex - cell.PathIndex) == 1;
            var right = TryGetPathIndex(cell.Row, cell.Column + 1, out var rightIndex) && Math.Abs(rightIndex - cell.PathIndex) == 1;
            var bottom = TryGetPathIndex(cell.Row + 1, cell.Column, out var bottomIndex) && Math.Abs(bottomIndex - cell.PathIndex) == 1;
            var left = TryGetPathIndex(cell.Row, cell.Column - 1, out var leftIndex) && Math.Abs(leftIndex - cell.PathIndex) == 1;
            cell.SetLinks(top, right, bottom, left);
        }

        var startCell = Cells.FirstOrDefault(static c => c.IsStart);
        if (startCell is not null)
            ConnectEndpoint(startCell, minCell, maxCell);

        var finishCell = Cells.FirstOrDefault(static c => c.IsFinish);
        if (finishCell is not null)
            ConnectEndpoint(finishCell, maxCell, minCell);
    }

    private void ConnectEndpoint(HamiltonianBoardCellViewModel endpoint, HamiltonianBoardCellViewModel primary, HamiltonianBoardCellViewModel secondary)
    {
        if (TryApplyLink(endpoint, primary))
            return;

        TryApplyLink(endpoint, secondary);
    }

    private bool TryApplyLink(HamiltonianBoardCellViewModel from, HamiltonianBoardCellViewModel to)
    {
        var dr = to.Row - from.Row;
        var dc = to.Column - from.Column;
        if (Math.Abs(dr) + Math.Abs(dc) != 1)
            return false;

        var fromTop = dr == -1;
        var fromRight = dc == 1;
        var fromBottom = dr == 1;
        var fromLeft = dc == -1;

        from.SetLinks(
            from.LinkTop || fromTop,
            from.LinkRight || fromRight,
            from.LinkBottom || fromBottom,
            from.LinkLeft || fromLeft);

        to.SetLinks(
            to.LinkTop || fromBottom,
            to.LinkRight || fromLeft,
            to.LinkBottom || fromTop,
            to.LinkLeft || fromRight);

        return true;
    }

    private bool TryGetPathIndex(int row, int column, out int index)
    {
        index = 0;
        if (row < 0 || column < 0 || row >= BoardHeight || column >= BoardWidth)
            return false;

        var cell = GetCell(row, column);
        if (cell.PathIndex <= 0)
            return false;

        index = cell.PathIndex;
        return true;
    }
}
