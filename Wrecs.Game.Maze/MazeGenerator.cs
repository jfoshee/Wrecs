namespace Wrecs.Game.Maze;

static class MazeGenerator
{
    // Randomized depth-first search (recursive back-tracker).
    public static Maze Generate(int width, int height, Random? random = null)
    {
        random ??= Random.Shared;
        var maze = new Maze(width, height);
        var visited = new bool[width, height];
        var stack = new Stack<(int X, int Y)>();

        var start = (X: 0, Y: 0);
        var goal = start;
        var greatestDepth = 1;
        visited[start.X, start.Y] = true;
        stack.Push(start);

        while (stack.Count > 0)
        {
            var current = stack.Peek();
            var neighbors = GetUnvisitedNeighbors(current, width, height, visited);

            if (neighbors.Count == 0)
            {
                stack.Pop();
                continue;
            }

            var (next, side) = neighbors[random.Next(neighbors.Count)];
            maze.RemoveWall(current.X, current.Y, side);
            maze.RemoveWall(next.X, next.Y, Opposite(side));
            visited[next.X, next.Y] = true;
            stack.Push(next);

            // A generated maze is a tree, so stack depth is also the unique
            // path distance from the start. Keep the most distant cell.
            if (stack.Count > greatestDepth)
            {
                greatestDepth = stack.Count;
                goal = next;
            }
        }

        maze.Goal = goal;
        return maze;
    }

    private static List<((int X, int Y) Cell, WallSides Side)> GetUnvisitedNeighbors(
        (int X, int Y) cell, int width, int height, bool[,] visited)
    {
        var result = new List<((int X, int Y), WallSides)>();

        void TryAdd(int dx, int dy, WallSides side)
        {
            var nx = cell.X + dx;
            var ny = cell.Y + dy;
            if (nx >= 0 && nx < width && ny >= 0 && ny < height && !visited[nx, ny])
            {
                result.Add(((nx, ny), side));
            }
        }

        TryAdd(0, -1, WallSides.North);
        TryAdd(1, 0, WallSides.East);
        TryAdd(0, 1, WallSides.South);
        TryAdd(-1, 0, WallSides.West);

        return result;
    }

    private static WallSides Opposite(WallSides side) => side switch
    {
        WallSides.North => WallSides.South,
        WallSides.South => WallSides.North,
        WallSides.East => WallSides.West,
        WallSides.West => WallSides.East,
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };
}
