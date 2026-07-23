using Wrecs.Core;
using Wrecs.Systems;

namespace Wrecs.Game.Maze;

class PlayerGoalCollisionHandler : ISystemEventHandler<AlignedRectangleCollisionEvent>
{
    public void HandleTyped(AlignedRectangleCollisionEvent e)
    {
        if (e.EntityA is PlayerAgent && e.EntityB is GoalEntity ||
            e.EntityB is PlayerAgent && e.EntityA is GoalEntity)
        {
            Console.WriteLine("Player reached the goal!");
            Environment.Exit(0);
        }
    }
}
