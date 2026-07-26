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
            var startingRectangle = _alignedRectangleSystem.GetTypedState(update.Entity);
            var testRectangle = startingRectangle.Rectangle.IsAlignedWith(update.State.Rectangle)
                ? startingRectangle.Rectangle.Sweep(update.State.Rectangle)
                : update.State.Rectangle;
            if (IsIntersectingWall(testRectangle))
            {
                return ConstraintResult.Reject();
            }
        }

        return ConstraintResult.Accept();
    }

    private bool IsIntersectingWall(AlignedRectangle rectangle)
    {
        // TODO: Only check nearby walls
        foreach (var wall in Maze.GetWalls())
        {
            if (rectangle.Intersects(wall))
            {
                return true;
            }
        }
        return false;
    }
}
