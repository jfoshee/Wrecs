using Wrecs.Core;
using Wrecs.Systems;

namespace Wrecs.Game.Maze;

class PlayerGoalCollisionHandler :
    ISystemEventHandler<CollisionEvent>,
    ISystemEventRaiser<EndGameEvent>
{
    private bool _goalReached = false;

    public IEnumerable<EndGameEvent> GetTypedEvents()
    {
        if (_goalReached)
        {
            yield return new EndGameEvent();
            _goalReached = false;
        }
    }

    public void HandleTyped(CollisionEvent e)
    {
        if (e.EntityA is PlayerAgent && e.EntityB is GoalEntity ||
            e.EntityB is PlayerAgent && e.EntityA is GoalEntity)
        {
            Console.WriteLine("Player reached the goal!");
            _goalReached = true;
        }
    }
}
