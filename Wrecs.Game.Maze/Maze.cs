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

class Maze
{
    public int Width { get; }
    public int Height { get; }

    private readonly WallSides[,] _walls;

    public Maze(int width, int height)
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

    public WallSides GetWalls(int x, int y) => _walls[x, y];

    public bool HasWall(int x, int y, WallSides side) => (_walls[x, y] & side) != 0;

    public void RemoveWall(int x, int y, WallSides side)
    {
        _walls[x, y] &= ~side;
    }
}
