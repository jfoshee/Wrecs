using Wrecs.Core;

namespace Wrecs.Systems;

public class AlignedRectangleCollisionEventSystem :
    ISystemEventRaiser<CollisionEvent>,
    ISystemInternalUpdatePreparer,
    IRequire<AlignedRectangleSystem>
{
    private AlignedRectangleSystem? _rectangleSystem;
    private readonly List<CollisionEvent> _events = [];

    public void Inject(AlignedRectangleSystem dependency) => _rectangleSystem = dependency;

    public void PrepareInternalUpdates()
    {
        if (_rectangleSystem is null)
            throw new InvalidOperationException($"{nameof(AlignedRectangleSystem)} is required for {nameof(CollisionEvent)}");

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
                    _events.Add(new CollisionEvent(entityA, entityB));
                }
            }
        }
    }

    public IEnumerable<CollisionEvent> GetTypedEvents()
    {
        var result = _events.ToList();
        _events.Clear();
        return result;
    }
}
