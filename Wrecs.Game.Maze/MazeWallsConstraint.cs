using Wrecs.Core;
using Wrecs.Geometry;
using Wrecs.Systems;

namespace Wrecs.Game.Maze;

class MazeWallsConstraint(ScaledMaze Maze) :
    ISystemConstraint,
    IRequire<AlignedRectangleSystem>
{
    private AlignedRectangleSystem _alignedRectangleSystem = null!;
    public void Inject(AlignedRectangleSystem dependency) => _alignedRectangleSystem = dependency;

    public ConstraintResult Validate(UpdateSet candidate)
    {
        foreach (var update in candidate.Updates.OfType<AlignedRectangleUpdate>())
        {
            var startingRectangle = _alignedRectangleSystem.GetTypedState(update.Entity).Rectangle;
            var endingRectangle = update.State.Rectangle;
            if (startingRectangle.IsAlignedWith(endingRectangle))
            {
                // Optimization: If the update is only a horizontal or vertical translation,
                // the path of the rectangle is also an aligned rectangle
                // So we can construct that rectangle and just check if it intersects any walls instead of a complex sweep test
                var sweepRectangle = startingRectangle.Sweep(endingRectangle);
                if (IsIntersectingWall(sweepRectangle))
                {
                    return ConstraintResult.Reject();
                }
            }
            else
            {
                // Otherwise we need to do a more complex sweep test, which is a bit more expensive
                if (IsRectPathIntersectingWall(startingRectangle, endingRectangle))
                {
                    return ConstraintResult.Reject();
                }
            }
        }

        return ConstraintResult.Accept();
    }

    private bool IsIntersectingWall(AlignedRectangle rectangle)
    {
        // TODO: Only check nearby walls
        foreach (var wall in Maze.GetWalls())
        {
            if (rectangle.OverlapsOrTouches(wall))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsRectPathIntersectingWall(AlignedRectangle start, AlignedRectangle end)
    {
        // TODO: Only check nearby walls
        foreach (var wall in Maze.GetWalls())
        {
            if (start.TrySweepIntersection(end, wall, out _))
            {
                return true;
            }
        }
        return false;
    }
}
