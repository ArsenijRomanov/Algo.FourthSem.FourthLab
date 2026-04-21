using System.Collections.ObjectModel;
using System.Diagnostics;
using SearchAlgorithms.UI.Shared.ViewModels;
using SlidingPuzzle.Core.Abstractions;
using SlidingPuzzle.Core.Domains;
using SlidingPuzzle.Core.Enums;
using SlidingPuzzle.Core.Solvers;

namespace SlidingPuzzle.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly Stack<byte[]> _undo = [];
    private readonly Random _random = new();
    private PuzzleBoard _board;
    private IReadOnlyList<Direction> _solutionMoves = [];
    private int _solutionIndex;
    private string _routeStatus = "Manual edit";
    private string _solveInfo = "Решение пока не найдено";
    private string _selectedAlgorithm = "A*";
    private bool _canUseSolutionControls;

    public MainWindowViewModel()
    {
        Algorithms = ["A*", "BFS", "IDA*", "Backjump DFS"];
        _board = CreateSolved();

        for (byte i = 0; i < 16; i++)
            Tiles.Add(new TileViewModel(i, _board[i]));

        ShuffleCommand = new RelayCommand(Shuffle);
        SolveCommand = new RelayCommand(Solve);
        UndoCommand = new RelayCommand(Undo, () => _undo.Count > 0);
        PrevStepCommand = new RelayCommand(PrevStep);
        NextStepCommand = new RelayCommand(NextStep);
        TileClickCommand = new RelayCommand<TileViewModel>(OnTileClick);

        MoveUpCommand = new RelayCommand(MoveBlankUp);
        MoveDownCommand = new RelayCommand(MoveBlankDown);
        MoveLeftCommand = new RelayCommand(MoveBlankLeft);
        MoveRightCommand = new RelayCommand(MoveBlankRight);
    }

    public ObservableCollection<TileViewModel> Tiles { get; } = [];
    public ObservableCollection<string> Algorithms { get; }

    public string SelectedAlgorithm
    {
        get => _selectedAlgorithm;
        set => SetProperty(ref _selectedAlgorithm, value);
    }

    public string RouteStatus { get => _routeStatus; set => SetProperty(ref _routeStatus, value); }
    public string SolveInfo { get => _solveInfo; set => SetProperty(ref _solveInfo, value); }
    public bool CanUseSolutionControls { get => _canUseSolutionControls; set => SetProperty(ref _canUseSolutionControls, value); }
    public string MovesPreview => _solutionMoves.Count == 0 ? "—" : string.Join(" ", _solutionMoves.Select(m => m.ToString()));

    public RelayCommand ShuffleCommand { get; }
    public RelayCommand SolveCommand { get; }
    public RelayCommand UndoCommand { get; }
    public RelayCommand PrevStepCommand { get; }
    public RelayCommand NextStepCommand { get; }
    public RelayCommand<TileViewModel> TileClickCommand { get; }
    public RelayCommand MoveUpCommand { get; }
    public RelayCommand MoveDownCommand { get; }
    public RelayCommand MoveLeftCommand { get; }
    public RelayCommand MoveRightCommand { get; }

    public void MoveBlankUp() => TryUserMove(Direction.Up);
    public void MoveBlankDown() => TryUserMove(Direction.Down);
    public void MoveBlankLeft() => TryUserMove(Direction.Left);
    public void MoveBlankRight() => TryUserMove(Direction.Right);

    private void Shuffle()
    {
        for (var i = 0; i < 150; i++)
        {
            var steps = _board.GetValidSteps().ToArray();
            _board.ApplyStep(steps[_random.Next(steps.Length)]);
        }

        _undo.Clear();
        InvalidateSolution("Manual edit");
        RefreshTiles();
    }

    private void Solve()
    {
        ISolver solver = SelectedAlgorithm switch
        {
            "BFS" => new BfsSolver(),
            "IDA*" => new IdaSolver(),
            "Backjump DFS" => new DfsBackJumpSolver(),
            _ => new AStarSolver()
        };

        var sw = Stopwatch.StartNew();
        var before = GC.GetTotalMemory(false);
        var result = solver.Solve(new PuzzleBoard(_board));
        var after = GC.GetTotalMemory(false);
        sw.Stop();

        _solutionMoves = result.Moves;
        _solutionIndex = 0;
        CanUseSolutionControls = result.IsSolved;
        RouteStatus = result.IsSolved ? "Following solution" : "Diverged";
        SolveInfo = result.IsSolved
            ? $"{SelectedAlgorithm}: {result.MoveCount} ходов, {sw.Elapsed.TotalMilliseconds:F2} ms, ΔMem {(after-before)/1024.0:F1} KB"
            : "Решение не найдено";

        RaisePropertyChanged(nameof(MovesPreview));
    }

    private void PrevStep()
    {
        if (!CanUseSolutionControls || _solutionIndex == 0)
            return;

        var prev = _solutionMoves[_solutionIndex - 1];
        _board.ApplyStep(GetOpposite(prev));
        _solutionIndex--;
        RefreshTiles();
    }

    private void NextStep()
    {
        if (!CanUseSolutionControls || _solutionIndex >= _solutionMoves.Count)
            return;

        var next = _solutionMoves[_solutionIndex];
        _board.ApplyStep(next);
        _solutionIndex++;
        RefreshTiles();
    }

    private void Undo()
    {
        if (_undo.Count == 0)
            return;

        var snapshot = _undo.Pop();
        _board = new PuzzleBoard(snapshot, 4, 4);
        RefreshTiles();
        UndoCommand.RaiseCanExecuteChanged();
    }

    private void OnTileClick(TileViewModel? tile)
    {
        if (tile is null || tile.Value == 0)
            return;

        var bx = _board.BlankTileX;
        var by = _board.BlankTileY;
        var tx = tile.Index % 4;
        var ty = tile.Index / 4;

        if (Math.Abs(bx - tx) + Math.Abs(by - ty) != 1)
            return;

        var dir = tx < bx ? Direction.Left : tx > bx ? Direction.Right : ty < by ? Direction.Up : Direction.Down;
        TryUserMove(dir);
    }

    private void TryUserMove(Direction dir)
    {
        if (!_board.IsValidStep(dir))
            return;

        _undo.Push(_board.ToArray());
        _board.ApplyStep(dir);
        RefreshTiles();
        UndoCommand.RaiseCanExecuteChanged();

        if (!CanUseSolutionControls)
        {
            RouteStatus = "Manual edit";
            return;
        }

        if (_solutionIndex < _solutionMoves.Count && _solutionMoves[_solutionIndex] == dir)
        {
            _solutionIndex++;
            RouteStatus = "Following solution";
            return;
        }

        if (_solutionIndex > 0 && GetOpposite(_solutionMoves[_solutionIndex - 1]) == dir)
        {
            _solutionIndex--;
            RouteStatus = "Following solution";
            return;
        }

        InvalidateSolution("Diverged");
    }

    private void InvalidateSolution(string state)
    {
        _solutionMoves = [];
        _solutionIndex = 0;
        CanUseSolutionControls = false;
        RouteStatus = state;
        RaisePropertyChanged(nameof(MovesPreview));
    }

    private static Direction GetOpposite(Direction dir) => dir switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        _ => dir
    };

    private static PuzzleBoard CreateSolved()
    {
        byte[] board = [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,0];
        return new PuzzleBoard(board, 4, 4);
    }

    private void RefreshTiles()
    {
        for (byte i = 0; i < 16; i++)
            Tiles[i].Value = _board[i];
    }
}
