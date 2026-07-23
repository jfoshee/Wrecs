using Wrecs.Core;

namespace Wrecs.Systems;

public record struct AlignedRectangleCollisionEvent(IEntity EntityA, IEntity EntityB) : IEvent;

public class AlignedRectangleCollisionEventSystem :
    ISystemEventRaiser<AlignedRectangleCollisionEvent>,
    ISystemInternalUpdatePreparer,
    IRequire<AlignedRectangleSystem>
{
    private AlignedRectangleSystem? _rectangleSystem;
    private readonly List<AlignedRectangleCollisionEvent> _events = [];

    public void Inject(AlignedRectangleSystem dependency) => _rectangleSystem = dependency;

    public void PrepareInternalUpdates()
    {
        if (_rectangleSystem is null)
            throw new InvalidOperationException($"{nameof(AlignedRectangleSystem)} is required for {nameof(AlignedRectangleCollisionEvent)}");

        // O(N^2)
        // We could do some spatial partitioning...
        // We could add a marker interface to denote entities that are "collidable" and only check those.
        var entities = _rectangleSystem.GetEntities();
        for (int i = 0; i < entities.Count; i++)
        {
            var entityA = entities[i];
            var rectA = _rectangleSystem.GetTypedState(entityA).Rectangle;
            for (int j = i + 1; j < entities.Count; j++)
            {
                var entityB = entities[j];
                var rectB = _rectangleSystem.GetTypedState(entityB).Rectangle;
                if (rectA.Intersects(rectB))
                {
                    _events.Add(new AlignedRectangleCollisionEvent(entityA, entityB));
                }
            }
        }
    }

    public IEnumerable<AlignedRectangleCollisionEvent> GetTypedEvents()
    {
        var result = _events.ToList();
        _events.Clear();
        return result;
    }
}
