using Wrecs.Core;
using Wrecs.Systems;

namespace Wrecs.Game.Maze;

class GameBoundsConstraint(float Width, float Height, float PlayerSize) : ISystemConstraint
{
    public ConstraintResult Validate(UpdateSet candidate)
    {
        foreach (var update in candidate.Updates)
        {
            if (update is Spatial2DUpdate spatialUpdate)
            {
                var pos = spatialUpdate.State.Position;
                if (pos.X < 0 || pos.Y < 0 || pos.X > Width - PlayerSize || pos.Y > Height - PlayerSize)
                {
                    return ConstraintResult.Reject();
                }
            }
        }
        return ConstraintResult.Accept();
    }
}
