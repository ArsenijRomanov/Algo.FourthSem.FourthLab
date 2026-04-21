namespace SlidingPuzzle.Core.Domains;

public sealed class PuzzleBoardKey : IEquatable<PuzzleBoardKey>
{
    private readonly byte[] _data;
    private readonly int _hashCode;

    public int Length => _data.Length;

    public PuzzleBoardKey(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        _data = (byte[])data.Clone();
        _hashCode = ComputeHashCode(_data);
    }

    public PuzzleBoardKey (PuzzleBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);

        _data = board.ToArray();
        _hashCode = ComputeHashCode(_data);
    }

    public bool Equals(PuzzleBoardKey? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null)
            return false;

        if (_hashCode != other._hashCode)
            return false;

        if (_data.Length != other._data.Length)
            return false;

        for (var i = 0; i < _data.Length; ++i)
        {
            if (_data[i] != other._data[i])
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is PuzzleBoardKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _hashCode;
    }

    private static int ComputeHashCode(byte[] data)
    {
        unchecked
        {
            var hash = 2166136261;

            for (int i = 0; i < data.Length; i++)
            {
                hash ^= data[i];
                hash *= 16777619;
            }

            return (int)hash;
        }
    }
}
