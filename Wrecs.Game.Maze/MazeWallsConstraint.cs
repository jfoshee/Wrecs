using Wrecs.Core;
using Wrecs.Geometry;
using Wrecs.Systems;

namespace Wrecs.Game.Maze;

class MazeWallsConstraint(Maze Maze, float MazeScale) : ISystemConstraint
{
    public ConstraintResult Validate(UpdateSet candidate)
    {
        foreach (var update in candidate.Updates)
        {
            if (update is AlignedRectangleUpdate rectUpdate)
            {
                if (IsIntersectingWall(rectUpdate.State.Rectangle))
                {
                    return ConstraintResult.Reject();
                }
            }
        }

        return ConstraintResult.Accept();
    }

    // TODO: Add AlignedLineSegment to Geometry and use it here to check for intersections with walls

    private bool IsIntersectingWall(AlignedRectangle rectangle)
    {
        var minCellX = (int)MathF.Floor(rectangle.Left / MazeScale);
        var maxCellX = (int)MathF.Floor(rectangle.Right / MazeScale);
        var minCellY = (int)MathF.Floor(rectangle.Bottom / MazeScale);
        var maxCellY = (int)MathF.Floor(rectangle.Top / MazeScale);

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                if (RectangleIntersectsCellWall(rectangle, cellX, cellY))
                    return true;
            }
        }

        return false;
    }

    private bool RectangleIntersectsCellWall(AlignedRectangle rectangle, int cellX, int cellY)
    {
        var walls = Maze.GetWalls(cellX, cellY);
        if (walls == WallSides.None)
            return false;

        var left = cellX * MazeScale;
        var right = left + MazeScale;
        var bottom = cellY * MazeScale;
        var top = bottom + MazeScale;
        var overlapsHorizontally = rectangle.Right >= left && rectangle.Left <= right;
        var overlapsVertically = rectangle.Top >= bottom && rectangle.Bottom <= top;

        return
            (overlapsVertically &&
             (((walls & WallSides.West) != 0 && rectangle.Left <= left && rectangle.Right >= left) ||
              ((walls & WallSides.East) != 0 && rectangle.Left <= right && rectangle.Right >= right))) ||
            (overlapsHorizontally &&
             (((walls & WallSides.North) != 0 && rectangle.Bottom <= bottom && rectangle.Top >= bottom) ||
              ((walls & WallSides.South) != 0 && rectangle.Bottom <= top && rectangle.Top >= top)));
    }
}
