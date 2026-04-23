using global::Avalonia.Controls;
using global::Avalonia.Media;
using System.Collections.ObjectModel;
using System.Text.Json;
using SlidingPuzzle.Avalonia.Models;
using SlidingPuzzle.Core.Abstractions;
using SlidingPuzzle.Core.Builders;
using SlidingPuzzle.Core.DataObjects;
using SlidingPuzzle.Core.Domains;
using SlidingPuzzle.Core.Enums;
using SlidingPuzzle.Core.Helpers;
using SlidingPuzzle.Core.Solvers;
using SearchAlgorithms.UI.Shared.Helpers;
using SearchAlgorithms.UI.Shared.Models;
using SearchAlgorithms.UI.Shared.Mvvm;
using SearchAlgorithms.UI.Shared.Services;

namespace SlidingPuzzle.Avalonia.ViewModels;

public sealed class SlidingPuzzleMainViewModel : ObservableObject
{
    private readonly BenchmarkService _benchmarkService;
    private readonly FileStorageService _fileStorageService;
    private readonly Stack<PuzzleUiSnapshot> _undoStack = [];
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private int _boardWidth = 4;
    private int _boardHeight = 4;
    private bool _isBusy;
    private bool _isEditMode;
    private string _randomShuffleMovesText = "20";
    private PlaybackStatus _playbackStatus = PlaybackStatus.None;
    private string _statusText = "Готово.";
    private PuzzleAlgorithmKind _selectedAlgorithm = PuzzleAlgorithmKind.AStar;
    private IReadOnlyList<Direction>? _solutionMoves;
    private List<byte[]>? _solutionStates;
    private int _currentStepIndex;
    private byte[] _tiles = [];
    private CancellationTokenSource? _runCancellationTokenSource;

    public SlidingPuzzleMainViewModel(BenchmarkService benchmarkService, FileStorageService fileStorageService)
    {
        _benchmarkService = benchmarkService;
        _fileStorageService = fileStorageService;

        Tiles = [];
        Results = [];
        RoutePreview = [];
        AlgorithmOptions = Enum.GetValues<PuzzleAlgorithmKind>();

        RandomizeCommand = new RelayCommand(RandomizeBoard);
        ResetSolvedCommand = new RelayCommand(ResetToSolved);
        UndoCommand = new RelayCommand(Undo, () => _undoStack.Count > 0);
        ClearResultsCommand = new RelayCommand(ClearResults, () => Results.Count > 0);
        RunCurrentCommand = new AsyncRelayCommand(RunCurrentAsync, () => !IsBusy);
        CancelRunCommand = new RelayCommand(CancelRun, () => IsBusy);
        StepForwardCommand = new RelayCommand(StepForward, () => CanStepForward);
        StepBackwardCommand = new RelayCommand(StepBackward, () => CanStepBackward);
        MoveUpCommand = new RelayCommand(() => TryManualMove(Direction.Up), () => !IsBusy && !IsEditMode);
        MoveDownCommand = new RelayCommand(() => TryManualMove(Direction.Down), () => !IsBusy && !IsEditMode);
        MoveLeftCommand = new RelayCommand(() => TryManualMove(Direction.Left), () => !IsBusy && !IsEditMode);
        MoveRightCommand = new RelayCommand(() => TryManualMove(Direction.Right), () => !IsBusy && !IsEditMode);

        ResetToSolved();
    }

    public ObservableCollection<PuzzleTileViewModel> Tiles { get; }
    public ObservableCollection<AlgorithmRunRecord> Results { get; }
    public ObservableCollection<MoveChipViewModel> RoutePreview { get; }
    public PuzzleAlgorithmKind[] AlgorithmOptions { get; }

    public RelayCommand RandomizeCommand { get; }
    public RelayCommand ResetSolvedCommand { get; }
    public RelayCommand UndoCommand { get; }
    public RelayCommand ClearResultsCommand { get; }
    public AsyncRelayCommand RunCurrentCommand { get; }
    public RelayCommand CancelRunCommand { get; }
    public RelayCommand StepForwardCommand { get; }
    public RelayCommand StepBackwardCommand { get; }
    public RelayCommand MoveUpCommand { get; }
    public RelayCommand MoveDownCommand { get; }
    public RelayCommand MoveLeftCommand { get; }
    public RelayCommand MoveRightCommand { get; }

    public int BoardWidth
    {
        get => _boardWidth;
        set
        {
            if (SetProperty(ref _boardWidth, Math.Clamp(value, 2, 6)))
                ResetToSolved();
        }
    }

    public int BoardHeight
    {
        get => _boardHeight;
        set
        {
            if (SetProperty(ref _boardHeight, Math.Clamp(value, 2, 6)))
                ResetToSolved();
        }
    }

    public string RandomShuffleMovesText
    {
        get => _randomShuffleMovesText;
        set => SetProperty(ref _randomShuffleMovesText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RunCurrentCommand.NotifyCanExecuteChanged();
                CancelRunCommand.NotifyCanExecuteChanged();
                MoveUpCommand.NotifyCanExecuteChanged();
                MoveDownCommand.NotifyCanExecuteChanged();
                MoveLeftCommand.NotifyCanExecuteChanged();
                MoveRightCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(IsInteractionEnabled));
            }
        }
    }

    public bool IsInteractionEnabled => !IsBusy;

    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            if (SetProperty(ref _isEditMode, value))
            {
                OnPropertyChanged(nameof(InteractionModeText));
                MoveUpCommand.NotifyCanExecuteChanged();
                MoveDownCommand.NotifyCanExecuteChanged();
                MoveLeftCommand.NotifyCanExecuteChanged();
                MoveRightCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string InteractionModeText => IsEditMode ? "Режим редактирования" : "Игровой режим";

    public PuzzleAlgorithmKind SelectedAlgorithm
    {
        get => _selectedAlgorithm;
        set => SetProperty(ref _selectedAlgorithm, value);
    }

    public PlaybackStatus PlaybackStatus
    {
        get => _playbackStatus;
        private set
        {
            if (SetProperty(ref _playbackStatus, value))
            {
                OnPropertyChanged(nameof(PlaybackStatusText));
                OnPropertyChanged(nameof(PlaybackBackgroundBrush));
                OnPropertyChanged(nameof(PlaybackBorderBrush));
                StepForwardCommand.NotifyCanExecuteChanged();
                StepBackwardCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string PlaybackStatusText => FormatHelper.PlaybackStatusText(PlaybackStatus);

    public IBrush PlaybackBackgroundBrush => PlaybackStatus switch
    {
        PlaybackStatus.FollowingSolution => new SolidColorBrush(Color.Parse("#153147")),
        PlaybackStatus.Diverged => new SolidColorBrush(Color.Parse("#3A2914")),
        PlaybackStatus.ManualEdit => new SolidColorBrush(Color.Parse("#30274A")),
        _ => new SolidColorBrush(Color.Parse("#252C39"))
    };

    public IBrush PlaybackBorderBrush => PlaybackStatus switch
    {
        PlaybackStatus.FollowingSolution => new SolidColorBrush(Color.Parse("#2B5E88")),
        PlaybackStatus.Diverged => new SolidColorBrush(Color.Parse("#9C6A21")),
        PlaybackStatus.ManualEdit => new SolidColorBrush(Color.Parse("#6C56B5")),
        _ => new SolidColorBrush(Color.Parse("#3B4558"))
    };

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public int CurrentStepIndex
    {
        get => _currentStepIndex;
        private set
        {
            if (SetProperty(ref _currentStepIndex, value))
            {
                OnPropertyChanged(nameof(ProgressText));
                UpdateRoutePreview();
                StepForwardCommand.NotifyCanExecuteChanged();
                StepBackwardCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string ProgressText => _solutionMoves is null
        ? "Маршрут не построен"
        : $"Шаг {CurrentStepIndex}/{_solutionMoves.Count}";

    public bool CanStepForward =>
        PlaybackStatus == PlaybackStatus.FollowingSolution &&
        _solutionMoves is not null &&
        CurrentStepIndex < _solutionMoves.Count;

    public bool CanStepBackward =>
        PlaybackStatus == PlaybackStatus.FollowingSolution &&
        CurrentStepIndex > 0;

    public async Task ImportBoardAsync(Window owner)
    {
        var text = await _fileStorageService.PickAndReadTextAsync(owner, "Импорт поля пятнашек", "json");
        if (string.IsNullOrWhiteSpace(text))
            return;

        var dto = JsonSerializer.Deserialize<PuzzleBoardFileDto>(text, _jsonOptions);
        if (dto?.Tiles is null || dto.Tiles.Length == 0)
            return;

        var width = dto.Width > 0 ? dto.Width : dto.Size;
        var height = dto.Height > 0 ? dto.Height : dto.Size;
        BoardWidth = width;
        BoardHeight = height;
        _undoStack.Clear();
        LoadBoard(dto.Tiles, clearPlayback: true);
        StatusText = "Поле импортировано.";
    }

    public async Task ExportBoardAsync(Window owner)
    {
        var dto = new PuzzleBoardFileDto
        {
            Size = Math.Max(BoardWidth, BoardHeight),
            Width = BoardWidth,
            Height = BoardHeight,
            Tiles = (byte[])_tiles.Clone()
        };

        var text = JsonSerializer.Serialize(dto, _jsonOptions);
        await _fileStorageService.SaveTextAsync(owner, "Экспорт поля пятнашек", "sliding-puzzle-board", ".json", text);
        StatusText = "Поле экспортировано.";
    }

    public void RandomizeBoard()
    {
        var builder = new PuzzleBoardBuilder()
            .WithSize((byte)BoardHeight, (byte)BoardWidth)
            .AllowGoal(false);

        var hasDepth = int.TryParse(RandomShuffleMovesText, out var depth) && depth > 0;
        var board = hasDepth
            ? builder.BuildRandomSolvableAtDistance(depth)
            : builder.BuildRandomSolvablePermutation();

        _undoStack.Clear();
        LoadBoard(board.ToArray(), clearPlayback: true);
        StatusText = hasDepth
            ? $"Сгенерировано поле на глубине {depth} от целевого состояния."
            : "Сгенерирована случайная перестановка.";
    }

    public void ResetToSolved()
    {
        var tiles = Enumerable.Range(1, BoardWidth * BoardHeight - 1)
            .Select(static x => (byte)x)
            .Append((byte)0)
            .ToArray();

        _undoStack.Clear();
        LoadBoard(tiles, clearPlayback: true);
        StatusText = "Загружена собранная раскладка.";
    }

    public void Undo()
    {
        if (_undoStack.Count == 0)
            return;

        var snapshot = _undoStack.Pop();
        _tiles = (byte[])snapshot.Tiles.Clone();
        RefreshTiles();
        PlaybackStatus = snapshot.PlaybackStatus;
        CurrentStepIndex = snapshot.CurrentStepIndex;
        IsEditMode = snapshot.IsEditMode;
        StatusText = "";
        UndoCommand.NotifyCanExecuteChanged();
    }

    public void ClearResults()
    {
        Results.Clear();
        ClearResultsCommand.NotifyCanExecuteChanged();
    }

    public bool TryHandleTileClick(int tileIndex)
    {
        if (IsEditMode)
            return false;

        if (!CanMoveTileIntoBlank(tileIndex, out var direction))
            return false;

        PushUndoSnapshot();
        ApplyBlankMove(direction);
        SyncPlaybackAfterManualMove(direction);
        StatusText = "Ручной ход применён.";
        return true;
    }

    public bool TrySwapTiles(int sourceIndex, int targetIndex)
    {
        if (!IsEditMode || sourceIndex == targetIndex)
            return false;

        PushUndoSnapshot();
        (_tiles[sourceIndex], _tiles[targetIndex]) = (_tiles[targetIndex], _tiles[sourceIndex]);
        RefreshTiles();
        PlaybackStatus = PlaybackStatus.ManualEdit;
        StatusText = "";
        return true;
    }

    public bool TryManualMove(Direction direction)
    {
        if (!CanMoveBlank(direction))
            return false;

        PushUndoSnapshot();
        ApplyBlankMove(direction);
        SyncPlaybackAfterManualMove(direction);
        StatusText = "";
        return true;
    }

    public void BeginDrag(int tileIndex)
    {
        if (!IsEditMode)
            return;

        foreach (var tile in Tiles)
        {
            tile.IsDragSource = tile.Index == tileIndex;
            tile.IsDragTarget = false;
        }
    }

    public void UpdateDragTarget(int tileIndex)
    {
        if (!IsEditMode)
            return;

        foreach (var tile in Tiles)
            tile.IsDragTarget = tile.Index == tileIndex;
    }

    public void ClearDragVisuals()
    {
        foreach (var tile in Tiles)
        {
            tile.IsDragSource = false;
            tile.IsDragTarget = false;
        }
    }

    private async Task RunCurrentAsync()
    {
        _runCancellationTokenSource?.Dispose();
        _runCancellationTokenSource = new CancellationTokenSource();
        IsBusy = true;

        try
        {
            var title = SelectedAlgorithm switch
            {
                PuzzleAlgorithmKind.BFS => "BFS",
                PuzzleAlgorithmKind.AStar => "A*",
                PuzzleAlgorithmKind.IdaStar => "IDA*",
                _ => "IDA* + Backjumping"
            };

            try
            {
                var snapshot = (byte[])_tiles.Clone();
                var width = BoardWidth;
                var height = BoardHeight;

                var benchmark = await Task.Run(() =>
                {
                    var board = new PuzzleBoard(snapshot, (byte)height, (byte)width);
                    var solver = CreateSolver(SelectedAlgorithm);
                    return _benchmarkService.Run(() => solver.Solve(board, _runCancellationTokenSource.Token));
                }, _runCancellationTokenSource.Token);

                ApplySolveResult(benchmark.Result);

                Results.Insert(0, new AlgorithmRunRecord
                {
                    Title = title,
                    IsSuccess = benchmark.Result.IsSolved,
                    StatusText = benchmark.Result.IsSolved ? "Решено" : "Решение не найдено",
                    Elapsed = benchmark.Elapsed,
                    ManagedMemoryDeltaBytes = Math.Max(0, benchmark.ManagedMemoryDeltaBytes),
                    WorkingSetDeltaBytes = benchmark.WorkingSetDeltaBytes,
                    Steps = benchmark.Result.MoveCount,
                    Note = benchmark.Result.IsSolved
                        ? $""
                        : "Маршрут не найден"
                });
                ClearResultsCommand.NotifyCanExecuteChanged();

                StatusText = benchmark.Result.IsSolved
                    ? "Решение загружено."
                    : "Решение не найдено.";
            }
            catch (OperationCanceledException)
            {
                StatusText = "Выполнение алгоритма прервано.";
            }
            catch (Exception)
            {
                Results.Insert(0, new AlgorithmRunRecord
                {
                    Title = title,
                    IsSuccess = false,
                    StatusText = "Расстановка неразрешима",
                    Elapsed = TimeSpan.Zero,
                    ManagedMemoryDeltaBytes = 0,
                    WorkingSetDeltaBytes = 0,
                    Steps = 0,
                    Note = ""
                });
                ClearResultsCommand.NotifyCanExecuteChanged();

                PlaybackStatus = PlaybackStatus.None;
                StatusText = "";
            }
        }
        finally
        {
            IsBusy = false;
            _runCancellationTokenSource?.Dispose();
            _runCancellationTokenSource = null;
        }
    }

    private void CancelRun()
        => _runCancellationTokenSource?.Cancel();

    private ISolver CreateSolver(PuzzleAlgorithmKind algorithm) => algorithm switch
    {
        PuzzleAlgorithmKind.BFS => new BfsSolver(),
        PuzzleAlgorithmKind.AStar => new AStarSolver(),
        PuzzleAlgorithmKind.IdaStar => new IdaSolver(),
        _ => new IdaBackJumpSolver()
    };

    private void ApplySolveResult(SolveResult result)
    {
        _solutionMoves = result.Moves;
        _solutionStates = BuildSolutionStates(_tiles, BoardHeight, BoardWidth, result.Moves).ToList();
        CurrentStepIndex = 0;
        PlaybackStatus = result.IsSolved ? PlaybackStatus.FollowingSolution : PlaybackStatus.None;
        BuildRoutePreview(result.Moves);
        StepForwardCommand.NotifyCanExecuteChanged();
        StepBackwardCommand.NotifyCanExecuteChanged();
    }

    private static IReadOnlyList<byte[]> BuildSolutionStates(byte[] initialBoard, int height, int width, IReadOnlyList<Direction> moves)
    {
        var states = new List<byte[]> { (byte[])initialBoard.Clone() };
        var temp = new PuzzleBoard((byte[])initialBoard.Clone(), (byte)height, (byte)width);

        foreach (var move in moves)
        {
            temp.ApplyStep(move);
            states.Add(temp.ToArray());
        }

        return states;
    }

    private void BuildRoutePreview(IReadOnlyList<Direction> moves)
    {
        RoutePreview.Clear();

        for (var i = 0; i < moves.Count; i++)
        {
            RoutePreview.Add(new MoveChipViewModel
            {
                StepIndex = i,
                Text = FormatDirection(moves[i]),
                IsActive = i == 0
            });
        }
    }

    private void UpdateRoutePreview()
    {
        for (var i = 0; i < RoutePreview.Count; i++)
            RoutePreview[i].IsActive = i == CurrentStepIndex;
    }

    private void StepForward()
    {
        if (_solutionMoves is null || CurrentStepIndex >= _solutionMoves.Count)
            return;

        PushUndoSnapshot();
        ApplyBlankMove(_solutionMoves[CurrentStepIndex]);
        CurrentStepIndex++;
        PlaybackStatus = PlaybackStatus.FollowingSolution;
        StatusText = "";
    }

    private void StepBackward()
    {
        if (_solutionMoves is null || CurrentStepIndex == 0)
            return;

        PushUndoSnapshot();
        CurrentStepIndex--;
        _tiles = (byte[])_solutionStates![CurrentStepIndex].Clone();
        RefreshTiles();
        PlaybackStatus = PlaybackStatus.FollowingSolution;
        StatusText = "";
    }

    private void SyncPlaybackAfterManualMove(Direction move)
    {
        if (_solutionMoves is null || _solutionStates is null)
        {
            PlaybackStatus = PlaybackStatus.None;
            return;
        }

        var nextIndex = CurrentStepIndex < _solutionMoves.Count && _solutionMoves[CurrentStepIndex] == move
            ? CurrentStepIndex + 1
            : -1;

        if (nextIndex != -1 && BoardsEqual(_tiles, _solutionStates[nextIndex]))
        {
            CurrentStepIndex = nextIndex;
            PlaybackStatus = PlaybackStatus.FollowingSolution;
            return;
        }

        if (CurrentStepIndex > 0)
        {
            var prevDirection = DirectionHelper.GetOppositeDirection(_solutionMoves[CurrentStepIndex - 1]);
            if (prevDirection == move && BoardsEqual(_tiles, _solutionStates[CurrentStepIndex - 1]))
            {
                CurrentStepIndex -= 1;
                PlaybackStatus = PlaybackStatus.FollowingSolution;
                return;
            }
        }

        PlaybackStatus = PlaybackStatus.Diverged;
    }

    private bool CanMoveBlank(Direction direction)
    {
        var blankIndex = FindBlankIndex();
        var row = blankIndex / BoardWidth;
        var col = blankIndex % BoardWidth;

        return direction switch
        {
            Direction.Left => col > 0,
            Direction.Right => col < BoardWidth - 1,
            Direction.Up => row > 0,
            Direction.Down => row < BoardHeight - 1,
            _ => false
        };
    }

    private void ApplyBlankMove(Direction direction)
    {
        var blankIndex = FindBlankIndex();
        var targetIndex = direction switch
        {
            Direction.Left => blankIndex - 1,
            Direction.Right => blankIndex + 1,
            Direction.Up => blankIndex - BoardWidth,
            Direction.Down => blankIndex + BoardWidth,
            _ => blankIndex
        };

        (_tiles[blankIndex], _tiles[targetIndex]) = (_tiles[targetIndex], _tiles[blankIndex]);
        RefreshTiles();
    }

    private bool CanMoveTileIntoBlank(int tileIndex, out Direction direction)
    {
        var blankIndex = FindBlankIndex();
        var blankRow = blankIndex / BoardWidth;
        var blankCol = blankIndex % BoardWidth;
        var tileRow = tileIndex / BoardWidth;
        var tileCol = tileIndex % BoardWidth;

        direction = Direction.Left;

        if (tileRow == blankRow && tileCol == blankCol + 1)
        {
            direction = Direction.Right;
            return true;
        }

        if (tileRow == blankRow && tileCol == blankCol - 1)
        {
            direction = Direction.Left;
            return true;
        }

        if (tileCol == blankCol && tileRow == blankRow + 1)
        {
            direction = Direction.Down;
            return true;
        }

        if (tileCol == blankCol && tileRow == blankRow - 1)
        {
            direction = Direction.Up;
            return true;
        }

        return false;
    }

    private void LoadBoard(byte[] tiles, bool clearPlayback)
    {
        _tiles = (byte[])tiles.Clone();
        RefreshTiles();

        if (clearPlayback)
        {
            _solutionMoves = null;
            _solutionStates = null;
            PlaybackStatus = PlaybackStatus.None;
            CurrentStepIndex = 0;
            RoutePreview.Clear();
        }

        UndoCommand.NotifyCanExecuteChanged();
    }

    private void RefreshTiles()
    {
        Tiles.Clear();
        for (var i = 0; i < _tiles.Length; i++)
            Tiles.Add(new PuzzleTileViewModel(i, _tiles[i]));
    }

    private int FindBlankIndex()
    {
        for (var i = 0; i < _tiles.Length; i++)
        {
            if (_tiles[i] == 0)
                return i;
        }

        throw new InvalidOperationException("Blank tile not found.");
    }

    private void PushUndoSnapshot()
    {
        _undoStack.Push(new PuzzleUiSnapshot
        {
            Tiles = (byte[])_tiles.Clone(),
            PlaybackStatus = PlaybackStatus,
            CurrentStepIndex = CurrentStepIndex,
            HasSolution = _solutionMoves is not null,
            IsEditMode = IsEditMode
        });

        UndoCommand.NotifyCanExecuteChanged();
    }

    private static bool BoardsEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
            return false;

        for (var i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i])
                return false;
        }

        return true;
    }

    private static string FormatDirection(Direction direction) => direction switch
    {
        Direction.Up => "Вверх",
        Direction.Down => "Вниз",
        Direction.Left => "Влево",
        Direction.Right => "Вправо",
        _ => direction.ToString()
    };
}
