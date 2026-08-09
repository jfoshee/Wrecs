using Wrecs.Core;
using Wrecs.Systems;

namespace Wrecs.Game.Polygons;

class ScreenBoundsConstraint(float Width, float Height) : ISystemConstraint
{
    public ConstraintResult Validate(UpdateSet candidate)
    {
        foreach (var update in candidate.Updates)
        {
            if (update is not CircleUpdate circleUpdate)
            {
                continue;
            }

            var circle = circleUpdate.State.Circle;
            if (circle.Center.X - circle.Radius < 0 ||
                circle.Center.Y - circle.Radius < 0 ||
                circle.Center.X + circle.Radius > Width ||
                circle.Center.Y + circle.Radius > Height)
            {
                return ConstraintResult.Reject();
            }
        }

        return ConstraintResult.Accept();
    }
}
