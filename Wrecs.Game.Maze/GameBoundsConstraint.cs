using Wrecs.Core;
using Wrecs.Systems;

namespace Wrecs.Game.Maze;

class GameBoundsConstraint : ISystemConstraint
{
    const float PlayerSize = 100;

    public ConstraintResult Validate(UpdateSet candidate)
    {
        foreach (var update in candidate.Updates)
        {
            if (update is Spatial2DUpdate spatialUpdate)
            {
                var pos = spatialUpdate.State.Position;
                if (pos.X < 0 || pos.Y < 0 || pos.X > 800 - PlayerSize || pos.Y > 600 - PlayerSize)
                {
                    return ConstraintResult.Reject();
                }
            }
        }
        return ConstraintResult.Accept();
    }
}
