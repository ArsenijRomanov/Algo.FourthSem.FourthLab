using SlidingPuzzle.Core.Enums;
using SlidingPuzzle.Core.Helpers;

namespace SlidingPuzzle.Core.Domains;

public class PuzzleBoard : IEquatable<PuzzleBoard>
{
    private byte[] _board;

    public byte Height { get; }
    public byte Width { get; }
    public byte BlankTilePosition { get; private set; }
    public int TotalManhattanDistance { get; private set; }
    public bool IsGoal => TotalManhattanDistance == 0;
    public byte BlankTileX => (byte)(BlankTilePosition % Width);
    public byte BlankTileY => (byte)(BlankTilePosition / Width);
    
    public PuzzleBoard(byte[] board, byte height, byte width)
    {
        if (!IsSolvable(board, height, width))
            throw new ArgumentException(null, nameof(board));
        
        _board = (byte[])board.Clone();
        Height = height;
        Width = width;
        BlankTilePosition = GetBlankTilePosition(_board);
        SetTotalManhattanDistance();
    }
    
    public PuzzleBoard(byte[,] board)
    {
        var height = (byte)board.GetLength(0);
        var width = (byte)board.GetLength(1);
        _board = board.Cast<byte>().ToArray();
        
        if (!IsSolvable(_board, height, width))
            throw new ArgumentException(null, nameof(board));

        Height = height;
        Width = width;
        BlankTilePosition = GetBlankTilePosition(_board);
        SetTotalManhattanDistance();
    }

    public PuzzleBoard(PuzzleBoard other)
    {
        _board = (byte[])other._board.Clone();
        Height = other.Height;
        Width = other.Width;
        BlankTilePosition = other.BlankTilePosition;
        TotalManhattanDistance = other.TotalManhattanDistance;
    }
    
    public static bool IsValidBoard(byte[] board, byte height, byte width)
    {
        ArgumentNullException.ThrowIfNull(board);

        if (height == 0 || width == 0)
            throw new ArgumentException(null, nameof(board));

        var area = height * width;
        if (area != board.Length || area > byte.MaxValue + 1)
            throw new ArgumentException(null, nameof(board));

        var length = board.Length;
        var set = new HashSet<byte>();
        var max = byte.MinValue;
        var min = byte.MaxValue;

        for (var i = 0; i < length; ++i)
        {
            max = byte.Max(max, board[i]);
            min = byte.Min(min, board[i]);
            set.Add(board[i]);
        }

        return set.Count == length && min == 0 && max == length - 1;
    }

    public static bool IsSolvable(byte[] board, byte height, byte width)
    {
        if (!IsValidBoard(board, height, width))
            throw new ArgumentException(null, nameof(board));

        if (width == 1 || height == 1)
        {
            byte expected = 1;

            foreach (var t in board)
            {
                if (t == 0)
                    continue;

                if (t != expected)
                    return false;

                expected++;
            }

            return true;
        }

        var blankRowFromBottom = -1;

        for (var i = 0; i < board.Length; ++i)
        {
            if (board[i] != 0)
                continue;

            blankRowFromBottom = height - i / width;
            break;
        }

        var invCnt = CountInversions(board);

        if (width % 2 == 1)
            return invCnt % 2 == 0;

        return (blankRowFromBottom + invCnt) % 2 == 1;
    }
    
    public byte this[byte y, byte x]
    {
        get
        {
            if (y >= Height || x >= Width)
                throw new ArgumentOutOfRangeException();

            return _board[Width * y + x];
        }
        set
        {
            if (y >= Height || x >= Width)
                throw new ArgumentOutOfRangeException();

            _board[Width * y + x] = value;
        }
    }
    
    public byte this[byte index]
    {
        get => _board[index];
        set => _board[index] = value;
    }

    public void ApplyStep(Direction dir)
    {
        Step(dir, inPlace: true);
    }

    public PuzzleBoard MakeStep(Direction dir)
    {
        return Step(dir, inPlace: false);
    }

    public bool TryApplyStep(Direction dir)
    {
        if (!IsValidStep(dir))
            return false;

        Step(dir, inPlace: true);
        return true;
    }

    public bool TryMakeStep(Direction dir, out PuzzleBoard? nextBoard)
    {
        if (!IsValidStep(dir))
        {
            nextBoard = null;
            return false;
        }

        nextBoard = Step(dir, inPlace: false);
        return true;
    }
    
    public void UndoStep(Direction dir)
    {
        ApplyStep(DirectionHelper.GetOppositeDirection(dir));
    }

    public bool IsValidStep(Direction dir)
    {
        return dir switch
        {
            Direction.Left => BlankTileX != 0,
            Direction.Down => BlankTileY != Height - 1,
            Direction.Right => BlankTileX != Width - 1,
            Direction.Up => BlankTileY != 0,
            _ => false
        };
    }
    
    public IEnumerable<Direction> GetValidSteps()
    {
        if (IsValidStep(Direction.Left))
            yield return Direction.Left;
        if (IsValidStep(Direction.Down))
            yield return Direction.Down;
        if (IsValidStep(Direction.Right))
            yield return Direction.Right;
        if (IsValidStep(Direction.Up))
            yield return Direction.Up;
    }

    public PuzzleBoardKey GetKey() => new PuzzleBoardKey(this);
    
    public bool Equals(PuzzleBoard? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null)
            return false;

        if (Height != other.Height || Width != other.Width)
            return false;

        for (var i = 0; i < _board.Length; ++i)
        {
            if (_board[i] != other._board[i])
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is PuzzleBoard other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Height;
            hash = hash * 31 + Width;

            foreach (var t in _board)
                hash = hash * 31 + t;

            return hash;
        }
    }
    
    public static bool operator ==(PuzzleBoard? left, PuzzleBoard? right)
        => Equals(left, right);

    public static bool operator !=(PuzzleBoard? left, PuzzleBoard? right)
        => !Equals(left, right);

    public byte[] ToArray() => (byte[])_board.Clone();
    
    private PuzzleBoard Step(Direction dir, bool inPlace)
    {
        if (!IsValidStep(dir))
            throw new ArgumentException(null, nameof(dir));

        var board = inPlace ? this : new PuzzleBoard(this);

        var oldBlankIndex = board.BlankTilePosition;
        var newBlankIndex = board.GetNewBlankPosition(dir);
        var movedTile = board._board[newBlankIndex];

        board.UpdateManhattanDistanceForMove(movedTile, newBlankIndex, oldBlankIndex);

        (board._board[oldBlankIndex], board._board[newBlankIndex]) =
            (board._board[newBlankIndex], board._board[oldBlankIndex]);

        board.BlankTilePosition = newBlankIndex;

        return board;
    }

    private byte GetNewBlankPosition(Direction dir)
    {
        return dir switch
        {
            Direction.Left => (byte)(BlankTilePosition - 1),
            Direction.Down => (byte)(BlankTilePosition + Width),
            Direction.Right => (byte)(BlankTilePosition + 1),
            Direction.Up => (byte)(BlankTilePosition - Width),
            _ => throw new ArgumentException(null, nameof(dir))
        };
    }
    
    private static int CountInversions(byte[] board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var inversions = 0;

        for (var i = 0; i < board.Length; ++i)
        {
            var left = board[i];
            if (left == 0)
                continue;

            for (var j = i + 1; j < board.Length; ++j)
            {
                var right = board[j];
                if (right == 0)
                    continue;

                if (left > right)
                    inversions++;
            }
        }

        return inversions;
    }
    
    private static byte GetBlankTilePosition(byte[] board)
    {
        for (var i = 0; i < board.Length; ++i)
        {
            if (board[i] == 0)
                return (byte)i;
        }

        throw new ArgumentException(null, nameof(board));
    }

    private void SetTotalManhattanDistance()
    {
        TotalManhattanDistance = 0;

        for (var index = 0; index < _board.Length; ++index)
        {
            var tileValue = _board[index];
            if (tileValue == 0)
                continue;

            TotalManhattanDistance += GetTileManhattanDistance(tileValue, (byte)index);
        }
    }
    
    private void UpdateManhattanDistanceForMove(byte tileValue, byte oldTileIndex, byte newTileIndex)
    {
        if (tileValue == 0)
            return;

        var oldDistance = GetTileManhattanDistance(tileValue, oldTileIndex);
        var newDistance = GetTileManhattanDistance(tileValue, newTileIndex);

        TotalManhattanDistance += newDistance - oldDistance;
    }

    private int GetTileManhattanDistance(byte tileValue, byte index)
    {
        var x = index % Width;
        var y = index / Width;

        var goalIndex = tileValue - 1;
        var goalX = goalIndex % Width;
        var goalY = goalIndex / Width;

        return Math.Abs(x - goalX) + Math.Abs(y - goalY);
    }
}
