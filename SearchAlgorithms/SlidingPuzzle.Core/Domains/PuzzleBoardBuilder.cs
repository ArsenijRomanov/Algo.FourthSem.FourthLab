using SlidingPuzzle.Core.Domains;
using SlidingPuzzle.Core.Enums;

namespace SlidingPuzzle.Core.Builders;

public class PuzzleBoardBuilder
{
    private byte _height = 4;
    private byte _width = 4;
    private int _shuffleStepCount = 100;
    private Random _random = Random.Shared;
    private bool _allowGoal = false;

    public PuzzleBoardBuilder WithSize(byte height, byte width)
    {
        if (height == 0 || width == 0)
            throw new ArgumentException();

        _height = height;
        _width = width;
        return this;
    }

    public PuzzleBoardBuilder WithShuffleStepCount(int shuffleStepCount)
    {
        if (shuffleStepCount < 0)
            throw new ArgumentOutOfRangeException(nameof(shuffleStepCount));

        _shuffleStepCount = shuffleStepCount;
        return this;
    }

    public PuzzleBoardBuilder WithRandom(Random random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
        return this;
    }

    public PuzzleBoardBuilder AllowGoal(bool allowGoal = true)
    {
        _allowGoal = allowGoal;
        return this;
    }

    public PuzzleBoard BuildSolved()
    {
        return new PuzzleBoard(CreateSolvedBoard(), _height, _width);
    }

    public PuzzleBoard BuildRandomSolvable()
    {
        var board = BuildSolved();

        if (_height == 1 && _width == 1)
            return board;

        Direction? previousDirection = null;

        for (var i = 0; i < _shuffleStepCount; ++i)
        {
            var dir = GetRandomStep(board, previousDirection);
            board.ApplyStep(dir);
            previousDirection = dir;
        }

        if (!_allowGoal && board.IsGoal)
        {
            var dir = GetRandomStep(board, previousDirection);
            board.ApplyStep(dir);
        }

        return board;
    }

    public PuzzleBoard BuildRandomSolvablePermutation()
    {
        var area = _height * _width;

        if (area == 1)
            return BuildSolved();

        if (_height == 1 || _width == 1)
            return BuildRandomSolvableOneDimensional(area);

        var board = CreateOrderedBoard(area);
        Shuffle(board);

        if (!PuzzleBoard.IsSolvable(board, _height, _width))
            SwapTwoNonBlankTiles(board);

        var result = new PuzzleBoard(board, _height, _width);

        if (!_allowGoal && result.IsGoal)
            return BuildRandomSolvablePermutation();

        return result;
    }

    public PuzzleBoard BuildRandomSolvableAtDistance(int distance)
    {
        if (distance < 0)
            throw new ArgumentOutOfRangeException(nameof(distance));

        if (distance == 0)
            return BuildSolved();

        const int maxAttempts = 128;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var board = BuildSolved();
            var visited = new HashSet<PuzzleBoardKey> { board.GetKey() };
            Direction? previousDirection = null;
            var success = true;

            for (var step = 0; step < distance; step++)
            {
                var candidates = GetDepthCandidates(board, previousDirection, visited);
                if (candidates.Count == 0)
                {
                    success = false;
                    break;
                }

                var chosen = candidates[_random.Next(candidates.Count)];
                board.ApplyStep(chosen);
                visited.Add(board.GetKey());
                previousDirection = chosen;
            }

            if (!success)
                continue;

            if (!_allowGoal && board.IsGoal)
                continue;

            return board;
        }

        throw new InvalidOperationException($"Unable to generate a solvable board at distance {distance} from the goal.");
    }

    private PuzzleBoard BuildRandomSolvableOneDimensional(int area)
    {
        var board = new byte[area];
        var blankIndex = _random.Next(area);

        byte value = 1;
        for (var i = 0; i < area; ++i)
        {
            if (i == blankIndex)
            {
                board[i] = 0;
                continue;
            }

            board[i] = value++;
        }

        var result = new PuzzleBoard(board, _height, _width);

        if (!_allowGoal && result.IsGoal)
            return BuildRandomSolvableOneDimensional(area);

        return result;
    }

    private byte[] CreateSolvedBoard()
    {
        var area = _height * _width;
        return CreateOrderedBoard(area);
    }

    private static byte[] CreateOrderedBoard(int area)
    {
        var board = new byte[area];

        for (var i = 0; i < area - 1; ++i)
            board[i] = (byte)(i + 1);

        board[area - 1] = 0;
        return board;
    }

    private void Shuffle(byte[] board)
    {
        for (var i = board.Length - 1; i > 0; --i)
        {
            var j = _random.Next(i + 1);
            (board[i], board[j]) = (board[j], board[i]);
        }
    }

    private static void SwapTwoNonBlankTiles(byte[] board)
    {
        var firstIndex = -1;
        var secondIndex = -1;

        for (var i = 0; i < board.Length; ++i)
        {
            if (board[i] == 0)
                continue;

            if (firstIndex == -1)
            {
                firstIndex = i;
                continue;
            }

            secondIndex = i;
            break;
        }

        if (firstIndex == -1 || secondIndex == -1)
            throw new InvalidOperationException();

        (board[firstIndex], board[secondIndex]) = (board[secondIndex], board[firstIndex]);
    }

    private List<Direction> GetDepthCandidates(PuzzleBoard board, Direction? previousDirection, HashSet<PuzzleBoardKey> visited)
    {
        var candidates = new List<Direction>();
        var oppositeDirection = previousDirection.HasValue
            ? Helpers.DirectionHelper.GetOppositeDirection(previousDirection.Value)
            : (Direction?)null;

        foreach (var direction in board.GetValidSteps())
        {
            if (oppositeDirection.HasValue && direction == oppositeDirection.Value)
                continue;

            if (!board.TryMakeStep(direction, out var nextBoard) || nextBoard is null)
                continue;

            if (visited.Contains(nextBoard.GetKey()))
                continue;

            candidates.Add(direction);
        }

        return candidates;
    }

    private Direction GetRandomStep(PuzzleBoard board, Direction? previousDirection)
    {
        var moves = new List<Direction>();
        var oppositeDirection = previousDirection.HasValue
            ? Helpers.DirectionHelper.GetOppositeDirection(previousDirection.Value)
            : (Direction?)null;

        foreach (var dir in board.GetValidSteps())
        {
            if (oppositeDirection.HasValue && dir == oppositeDirection.Value)
                continue;

            moves.Add(dir);
        }

        if (moves.Count == 0)
        {
            foreach (var dir in board.GetValidSteps())
                moves.Add(dir);
        }

        return moves[_random.Next(moves.Count)];
    }
}
