using System.Numerics;
using Wrecs.Geometry;

namespace Wrecs.Game.Maze;

[Flags]
enum WallSides
{
    None = 0,
    Bottom = 1 << 0,
    Right = 1 << 1,
    Top = 1 << 2,
    Left = 1 << 3,
    All = Bottom | Right | Top | Left,
}

/// <summary>
/// Represents a maze as a grid of cells, each with walls on its sides.
/// The maze has integral size because it is generated as a grid of cells.
/// </summary>
class GridMaze
{
    public int Columns { get; }
    public int Rows { get; }
    public (int X, int Y) Goal { get; internal set; }

    private readonly WallSides[,] _walls;

    public GridMaze(int columns, int rows)
    {
        Columns = columns;
        Rows = rows;
        _walls = new WallSides[columns, rows];
        for (var x = 0; x < columns; x++)
        {
            for (var y = 0; y < rows; y++)
            {
                _walls[x, y] = WallSides.All;
            }
        }
    }

    public WallSides GetWalls(int x, int y)
    {
        // To simplify collision detection, allow requesting walls for out-of-bounds cells, which will return None (no walls).
        if (x < 0 || x >= Columns || y < 0 || y >= Rows)
            return WallSides.None;
        return _walls[x, y];
    }


    public bool HasWall(int x, int y, WallSides side) => (_walls[x, y] & side) != 0;

    public void RemoveWall(int x, int y, WallSides side)
    {
        _walls[x, y] &= ~side;
    }
}

class ScaledMaze(GridMaze Maze, float Scale)
{
    public float Width => Maze.Columns * Scale;
    public float Height => Maze.Rows * Scale;
    public Vector2 GoalPosition => new(Maze.Goal.X * Scale, Maze.Goal.Y * Scale);

    public IEnumerable<AxisAlignedSegment2> GetWalls()
    {
        // TODO: Cache wall segments
        // TODO: We can alternate which cells we check to avoid double-returning walls because most walls are shared between two cells; we can ignore exterior maze walls because maze bounds are handled elsewhere
        for (var x = 0; x < Maze.Columns; x++)
        {
            for (var y = 0; y < Maze.Rows; y++)
            {
                var left = x * Scale;
                var right = left + Scale;
                var bottom = y * Scale;
                var top = bottom + Scale;

                if (Maze.HasWall(x, y, WallSides.Top))
                    yield return new AxisAlignedSegment2(Axis2.X, new(0, top), new Interval(left, right));
                if (Maze.HasWall(x, y, WallSides.Right))
                    yield return new AxisAlignedSegment2(Axis2.Y, new(right, 0), new Interval(bottom, top));
                if (Maze.HasWall(x, y, WallSides.Bottom))
                    yield return new AxisAlignedSegment2(Axis2.X, new(0, bottom), new Interval(left, right));
                if (Maze.HasWall(x, y, WallSides.Left))
                    yield return new AxisAlignedSegment2(Axis2.Y, new(left, 0), new Interval(bottom, top));
            }
        }
    }
}
