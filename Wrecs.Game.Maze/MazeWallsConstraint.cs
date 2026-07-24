using System.Numerics;
using Wrecs.Core;
using Wrecs.Systems;

namespace Wrecs.Game.Maze;

class MazeWallsConstraint(Maze Maze, float MazeScale, float PlayerSize) : ISystemConstraint
{
    public ConstraintResult Validate(UpdateSet candidate)
    {
        foreach (var update in candidate.Updates)
        {
            if (update is AlignedRectangleUpdate rectUpdate)
            {
                // TODO: Use rectangle to do the collision detection
                var position = rectUpdate.State.Rectangle.BottomLeft;
                if (IsIntersectingWall(position))
                {
                    return ConstraintResult.Reject();
                }
            }
        }

        return ConstraintResult.Accept();
    }

    private bool IsIntersectingWall(Vector2 playerPosition)
    {
        // The player position is just the top-left corner of the player square.
        // We need to check the cell that the player is in, and the cells to the right and below, since the player can overlap those walls.
        var cell_x = (int)(playerPosition.X / MazeScale);
        var cell_y = (int)(playerPosition.Y / MazeScale);

        List<(int, int)> cells = [(cell_x, cell_y), (cell_x + 1, cell_y), (cell_x, cell_y + 1)];
        foreach (var cell in cells)
        {
            if (PlayerIntersectsCellWall(playerPosition, cell))
                return true;
        }
        return false;
    }

    private bool PlayerIntersectsCellWall(Vector2 playerPosition, (int, int) cell)
    {
        var p_x1 = playerPosition.X;
        var p_x2 = playerPosition.X + PlayerSize;
        var p_y1 = playerPosition.Y;
        var p_y2 = playerPosition.Y + PlayerSize;

        var (cell_x, cell_y) = cell;
        var walls = Maze.GetWalls(cell_x, cell_y);
        // Check if 2 horizontal sides of player intersect with vertical walls of the maze
        if (walls.HasFlag(WallSides.West))
        {
            var w_x = cell_x * MazeScale;
            var w_y1 = cell_y * MazeScale;
            var w_y2 = (cell_y + 1) * MazeScale;

            if (AAIntersectingEdge(p_x1, p_x2, p_y1, w_x, w_y1, w_y2))
                return true;
            if (AAIntersectingEdge(p_x1, p_x2, p_y2, w_x, w_y1, w_y2))
                return true;
        }
        if (walls.HasFlag(WallSides.East))
        {
            var w_x = (cell_x + 1) * MazeScale;
            var w_y1 = cell_y * MazeScale;
            var w_y2 = (cell_y + 1) * MazeScale;

            if (AAIntersectingEdge(p_x1, p_x2, p_y1, w_x, w_y1, w_y2))
                return true;
            if (AAIntersectingEdge(p_x1, p_x2, p_y2, w_x, w_y1, w_y2))
                return true;
        }

        // or if 2 vertical sides of player intersect with horizontal walls
        if (walls.HasFlag(WallSides.North))
        {
            var w_y = cell_y * MazeScale;
            var w_x1 = cell_x * MazeScale;
            var w_x2 = (cell_x + 1) * MazeScale;

            if (AAIntersectingEdge(w_x1, w_x2, w_y, p_x1, p_y1, p_y2))
                return true;
            if (AAIntersectingEdge(w_x1, w_x2, w_y, p_x2, p_y1, p_y2))
                return true;
        }
        if (walls.HasFlag(WallSides.South))
        {
            var w_y = (cell_y + 1) * MazeScale;
            var w_x1 = cell_x * MazeScale;
            var w_x2 = (cell_x + 1) * MazeScale;

            if (AAIntersectingEdge(w_x1, w_x2, w_y, p_x1, p_y1, p_y2))
                return true;
            if (AAIntersectingEdge(w_x1, w_x2, w_y, p_x2, p_y1, p_y2))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a horizontal edge intersects with a vertical edge.
    /// </summary>
    private static bool AAIntersectingEdge(float h_edge_x1, float h_edge_x2, float h_edge_y, float v_edge_x, float v_edge_y1, float v_edge_y2)
    {
        return
            // Vertical X between horizontal Xs, and
            v_edge_x >= h_edge_x1 && v_edge_x <= h_edge_x2 &&
            // Horizontal Y between vertical Ys
            h_edge_y >= v_edge_y1 && h_edge_y <= v_edge_y2;
    }
}