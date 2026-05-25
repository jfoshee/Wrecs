using Wrecs.Core;

namespace Wrecs.Systems;

/// <summary>
/// Raised whenever an entity wraps around from one end of a 1D space to the other
/// </summary>
public record struct WrapAround1DEvent(IEntity Entity, int OldPosition, int NewPosition) : IEvent;

public class WrapAroundSystem1D(int size) :
    IPrepareSharedUpdates,
    IRequire<Spatial1DSystem>,
    IRaise<WrapAround1DEvent>
{
    private Spatial1DSystem? _spatial1dSystem;
    private readonly List<WrapAround1DEvent> _events = [];

    public void Inject(Spatial1DSystem dependency) => _spatial1dSystem = dependency;

    public IEnumerable<UpdateSet> PrepareSharedUpdates()
    {
        if (_spatial1dSystem is null)
            throw new InvalidOperationException($"{nameof(Spatial1DSystem)} is required for {nameof(WrapAroundSystem1D)}");

        var updates = new List<EntityUpdate<PositionSnapshot>>();

        foreach (var entity in _spatial1dSystem.GetEntities())
        {
            var p = _spatial1dSystem.GetTypedState(entity).Position;
            if (p < 0 || p >= size)
            {
                var newPos = ((p % size) + size) % size;
                updates.Add(new EntityUpdate<PositionSnapshot>(entity, new PositionSnapshot(newPos)));
                _events.Add(new WrapAround1DEvent(entity, p, newPos));
            }
        }

        if (updates.Count > 0)
            yield return new UpdateSet([.. updates]);
    }

    public IEnumerable<WrapAround1DEvent> GetTypedEvents()
    {
        var result = _events.ToList();
        _events.Clear();
        return result;
    }
}
