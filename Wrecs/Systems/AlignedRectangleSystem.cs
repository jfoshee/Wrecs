using Wrecs.Core;
using Wrecs.Geometry;

namespace Wrecs.Systems;

public record struct AlignedRectangleSnapshot(AlignedRectangle Rectangle)
    : IStateSnapshot<AlignedRectangleSystem>
{
    public static implicit operator AlignedRectangle(AlignedRectangleSnapshot snapshot) =>
        snapshot.Rectangle;

    public static implicit operator AlignedRectangleSnapshot(AlignedRectangle rectangle) =>
        new(rectangle);
}

public record AlignedRectangleUpdate : EntityUpdate<AlignedRectangleSnapshot>
{
    public AlignedRectangleUpdate(IEntity entity, AlignedRectangle rectangle)
        : base(entity, new AlignedRectangleSnapshot(rectangle))
    {
    }
}

/// <summary>
/// Tracks an axis-aligned rectangle for each participating entity.
/// Entities without an initial rectangle use <see cref="AlignedRectangle.Empty"/>.
/// </summary>
public class AlignedRectangleSystem :
    ISystemWithEntities<IEntity, AlignedRectangleSnapshot>,
    ISystemAgentContextProvider<AlignedRectangleSnapshot>,
    ISystemUpdateAcceptor<AlignedRectangleSnapshot>
{
    private readonly List<IEntity> _entities = [];
    private readonly Dictionary<IEntity, AlignedRectangle> _rectangles = [];

    public IReadOnlyList<IEntity> GetEntities() => _entities;

    public AlignedRectangleSnapshot GetTypedState(IEntity entity) => new(_rectangles[entity]);

    public void InitEntities(
        params (IEntity entity, AlignedRectangleSnapshot? initialState)[] initialEntities)
    {
        _entities.Clear();
        _rectangles.Clear();

        foreach (var (entity, initialState) in initialEntities)
        {
            // TODO: only add entities that have initial state or implement marker interface
            _entities.Add(entity);
            _rectangles[entity] = initialState?.Rectangle ?? AlignedRectangle.Empty;
        }
    }

    public AlignedRectangleSnapshot? BuildSnapshot(IAgent agent) =>
        _rectangles.TryGetValue(agent, out var rectangle)
            ? new(rectangle)
            : null;

    public void ApplyUpdates(IEnumerable<EntityUpdate<AlignedRectangleSnapshot>> updates)
    {
        foreach (var update in updates)
            _rectangles[update.Entity] = update.State.Rectangle;
    }
}
