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

        var west = cellX * MazeScale;
        var east = west + MazeScale;
        var north = cellY * MazeScale;
        var south = north + MazeScale;
        var horizontalInterval = new Interval(west, east);
        var verticalInterval = new Interval(north, south);

        return
            (walls.HasFlag(WallSides.North) &&
             rectangle.Intersects(new AxisAlignedSegment2(Axis2.X, new(0, north), horizontalInterval))) ||
            (walls.HasFlag(WallSides.East) &&
             rectangle.Intersects(new AxisAlignedSegment2(Axis2.Y, new(east, 0), verticalInterval))) ||
            (walls.HasFlag(WallSides.South) &&
             rectangle.Intersects(new AxisAlignedSegment2(Axis2.X, new(0, south), horizontalInterval))) ||
            (walls.HasFlag(WallSides.West) &&
             rectangle.Intersects(new AxisAlignedSegment2(Axis2.Y, new(west, 0), verticalInterval)));
    }
}
