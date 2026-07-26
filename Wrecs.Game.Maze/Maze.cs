using System.Numerics;
using Wrecs.Geometry;

namespace Wrecs.Game.Maze;

[Flags]
enum WallSides
{
    None = 0,
    North = 1 << 0,
    East = 1 << 1,
    South = 1 << 2,
    West = 1 << 3,
    All = North | East | South | West,
}

/// <summary>
/// Represents a maze as a grid of cells, each with walls on its sides.
/// The maze has integral size because it is generated as a grid of cells.
/// </summary>
class GridMaze
{
    public int Width { get; }
    public int Height { get; }
    public (int X, int Y) Goal { get; internal set; }

    private readonly WallSides[,] _walls;

    public GridMaze(int width, int height)
    {
        Width = width;
        Height = height;
        _walls = new WallSides[width, height];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                _walls[x, y] = WallSides.All;
            }
        }
    }

    public WallSides GetWalls(int x, int y)
    {
        // To simplify collision detection, allow requesting walls for out-of-bounds cells, which will return None (no walls).
        if (x < 0 || x >= Width || y < 0 || y >= Height)
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
    public float Width => Maze.Width * Scale;
    public float Height => Maze.Height * Scale;
    public Vector2 GoalPosition => new(Maze.Goal.X * Scale, Maze.Goal.Y * Scale);

    public IEnumerable<AxisAlignedSegment2> GetWalls()
    {
        // TODO: Cache wall segments
        for (var x = 0; x < Maze.Width; x++)
        {
            for (var y = 0; y < Maze.Height; y++)
            {
                var left = x * Scale;
                var right = left + Scale;
                var bottom = y * Scale;
                var top = bottom + Scale;

                if (Maze.HasWall(x, y, WallSides.South))
                    yield return new AxisAlignedSegment2(Axis2.X, new(0, top), new Interval(left, right));
                if (Maze.HasWall(x, y, WallSides.East))
                    yield return new AxisAlignedSegment2(Axis2.Y, new(right, 0), new Interval(bottom, top));
                if (Maze.HasWall(x, y, WallSides.North))
                    yield return new AxisAlignedSegment2(Axis2.X, new(0, bottom), new Interval(left, right));
                if (Maze.HasWall(x, y, WallSides.West))
                    yield return new AxisAlignedSegment2(Axis2.Y, new(left, 0), new Interval(bottom, top));
            }
        }
    }
}
