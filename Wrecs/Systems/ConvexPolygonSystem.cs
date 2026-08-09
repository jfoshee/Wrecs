using Wrecs.Core;
using Wrecs.Geometry;

namespace Wrecs.Systems;

public interface IConvexPolygonEntity : IEntity;

public record struct ConvexPolygonSnapshot(ConvexPolygon Polygon)
    : IStateSnapshot<ConvexPolygonSystem>
{
    public static implicit operator ConvexPolygon(ConvexPolygonSnapshot snapshot) =>
        snapshot.Polygon;

    public static implicit operator ConvexPolygonSnapshot(ConvexPolygon polygon) =>
        new(polygon);
}

public record ConvexPolygonUpdate : EntityUpdate<ConvexPolygonSnapshot>
{
    public ConvexPolygonUpdate(IEntity entity, ConvexPolygon polygon)
        : base(entity, new ConvexPolygonSnapshot(polygon))
    {
    }
}

/// <summary>
/// Tracks a convex polygon for each participating entity.
/// Entities must provide an initial polygon.
/// </summary>
public class ConvexPolygonSystem :
    ISystemWithEntities<IConvexPolygonEntity, ConvexPolygonSnapshot>,
    ISystemAgentContextProvider<ConvexPolygonSnapshot>,
    ISystemAgentIntentTranslator<Move2DAction>,
    ISystemUpdateAcceptor<ConvexPolygonSnapshot>,
    ISystemLinkPositionSource,
    ISystemLinkPositionTarget
{
    private readonly Dictionary<IEntity, ConvexPolygon> _polygons = [];

    public IReadOnlyList<IEntity> GetEntities() => [.. _polygons.Keys];

    public ConvexPolygonSnapshot GetTypedState(IEntity entity) => new(_polygons[entity]);

    public void InitEntities(params (IEntity entity, ConvexPolygonSnapshot? initialState)[] initialEntities)
    {
        _polygons.Clear();
        foreach (var (entity, initialState) in initialEntities)
        {
            if (initialState is null)
            {
                throw new InvalidOperationException(
                    $"Entity {entity.Name} must provide an initial polygon for {nameof(ConvexPolygonSystem)}");
            }

            _polygons[entity] = initialState.Value.Polygon;
        }
    }

    public ConvexPolygonSnapshot? BuildSnapshot(IAgent agent) =>
        _polygons.TryGetValue(agent, out var polygon)
            ? new(polygon)
            : null;

    public void ApplyUpdates(IEnumerable<EntityUpdate<ConvexPolygonSnapshot>> updates)
    {
        foreach (var update in updates)
            _polygons[update.Entity] = update.State.Polygon;
    }

    Vector2 ISystemLinkPositionSource.GetPosition(IEntity entity) =>
        _polygons[entity].Bounds.Center;

    void ISystemLinkPositionTarget.SetPosition(IEntity entity, Vector2 position)
    {
        var polygon = _polygons[entity];
        var currentCenter = polygon.Bounds.Center;
        if (currentCenter == position)
            return;

        var delta = position - currentCenter;
        _polygons[entity] = TranslatePolygon(polygon, delta);
    }

    public UpdateSet TranslateIntent(IAgent agent, Move2DAction action)
    {
        if (!_polygons.TryGetValue(agent, out var polygon))
        {
            throw new InvalidOperationException(
                $"Agent {agent.Name} does not have a polygon in {nameof(ConvexPolygonSystem)}");
        }

        var moved = TranslatePolygon(polygon, action.Step);
        return new UpdateSet([new ConvexPolygonUpdate(agent, moved)]);
    }

    private static ConvexPolygon TranslatePolygon(ConvexPolygon polygon, Vector2 delta)
    {
        var movedVertices = new Vector2[polygon.Count];
        for (var i = 0; i < polygon.Count; i++)
            movedVertices[i] = polygon.GetVertex(i) + delta;

        return new ConvexPolygon(movedVertices);
    }
}