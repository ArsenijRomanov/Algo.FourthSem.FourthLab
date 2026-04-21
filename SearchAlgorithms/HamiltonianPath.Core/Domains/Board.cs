using HamiltonianPath.Core.Helpers;

namespace HamiltonianPath.Core.Domains;

public class Board
{
    private readonly int[,] _matrix;
    private const int WallNumber = -100;
    public int Height { get; }
    public int Width { get; }
    public Point Start { get; }
    public Point Finish { get; }
    public int FreePlacesCount { get; private set; }

    public Board(int[,] matrix, Point start, Point finish)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        
        Height = matrix.GetLength(0);
        Width = matrix.GetLength(1);
        
        if (!Contains(start))
            throw new ArgumentOutOfRangeException(nameof(start));
        if (!Contains(finish))
            throw new ArgumentOutOfRangeException(nameof(finish));
        if (matrix[start.Y, start.X] != 0)
            throw new ArgumentException(null, nameof(start));
        if (matrix[finish.Y, finish.X] != 0)
            throw new ArgumentException(null, nameof(finish));

        var freePlacesCount = 0;
        for (var y = 0; y < Height; ++y)
        {
            for (var x = 0; x < Width; ++x)
            {
                if (matrix[y, x] != WallNumber && matrix[y, x] != 0)
                    throw new ArgumentException(null, nameof(matrix));

                if (matrix[y, x] == 0)
                    ++freePlacesCount;
            }
        }
        
        _matrix = matrix;
        Start = start;
        Finish = finish;
        FreePlacesCount = freePlacesCount;
    }

    public Board(int height, int width, Point start, Point finish)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);

        Height = height;
        Width = width;
        
        if (!Contains(start))
            throw new ArgumentOutOfRangeException(nameof(start));
        if (!Contains(finish))
            throw new ArgumentOutOfRangeException(nameof(finish));
        
        _matrix = new int[height, width];
        Start = start;
        Finish = finish;
        FreePlacesCount = height * width;
    }

    public int this[int y, int x]
    {
        get => _matrix[y, x];
        set => _matrix[y, x] = value;
    }
    public int this[Point point]
    {
        get => _matrix[point.Y, point.X];
        set => _matrix[point.Y, point.X] = value;
    }

    public void Mark(Point point, int value = 1) => this[point] = value;
    
    public void Unmark(Point point) => this[point] = 0;

    public void SetVisited(Point point) => this[point] = 1;
    
    public void SetWall(Point point)
    {
        if (point == Start || point == Finish)
            throw new InvalidOperationException();
        
        if (this[point] == WallNumber) return;
        this[point] = WallNumber;
        --FreePlacesCount;
    }

    public bool IsVisited(Point point) => this[point] == 1;
    
    public bool IsFree(Point point) => this[point] == 0;

    public bool Contains(Point point)
        => point.X >= 0 && point.X < Width && 
           point.Y >= 0 && point.Y < Height;
    
    public bool Contains(int y, int x)
        => x < Width && x >= 0 && y < Height && y >= 0;

    public bool TryStep(Point point, Direction dir, out Point nextPoint)
    {
        if (dir == Direction.None)
            throw new ArgumentException(null, nameof(dir));
        
        var (dx, dy) = DirectionHelper.GetOffset(dir);
        var nextX = point.X + dx;
        var nextY = point.Y + dy;

        if (Contains(nextY, nextX) && _matrix[nextY, nextX] == 0)
        {
            nextPoint = new Point(nextX, nextY);
            return true;
        }

        nextPoint = default;
        return false;
    }

    public Point Step(Point point, Direction dir)
    {
        if (dir == Direction.None)
            throw new ArgumentException(null, nameof(dir));

        var (dx, dy) = DirectionHelper.GetOffset(dir);
        var nextX = point.X + dx;
        var nextY = point.Y + dy;

        return Contains(nextY, nextX) && _matrix[nextY, nextX] == 0
            ? new Point(nextX, nextY) 
            : throw new ArgumentException(null, nameof(point));
    }

    public Direction CalculateDirsMask(Point point)
    {
        var mask = Direction.None;

        if (TryStep(point, Direction.Left, out _))
            mask |= Direction.Left;

        if (TryStep(point, Direction.Down, out _))
            mask |= Direction.Down;

        if (TryStep(point, Direction.Right, out _))
            mask |= Direction.Right;

        if (TryStep(point, Direction.Up, out _))
            mask |= Direction.Up;

        return mask;
    }
}
