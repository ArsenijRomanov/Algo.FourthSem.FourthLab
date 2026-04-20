using GridSearch.Core.Helpers;

namespace GridSearch.Core.Domains;

public class Board
{
    private readonly int[,] _matrix;
    public int Height { get; }
    public int Width { get; }
    
    public Point Start { get; }
    
    public Point Finish { get; }

    public Board(int[,] matrix, Point start, Point finish)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        
        Height = matrix.GetLength(0);
        Width = matrix.GetLength(1);
        _matrix = matrix;
        Start = start;
        Finish = finish;
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

    public void Mark(Point point, int value) => this[point] = value;
    
    public void Unmark(Point point) => this[point] = 0;

    public void SetVisited(Point point) => this[point] = 1;

    public bool IsVisited(Point point) => this[point] == 1;

    public bool Contains(Point point)
        => point.X < Width && point.Y < Height;
    
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
