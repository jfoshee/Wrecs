using Wrecs.Core;
using Wrecs.Geometry;

namespace Wrecs.Systems;

public interface ICircleEntity : IEntity;

public record struct CircleSnapshot(Circle Circle)
    : IStateSnapshot<CircleSystem>
{
    public static implicit operator Circle(CircleSnapshot snapshot) => snapshot.Circle;

    public static implicit operator CircleSnapshot(Circle circle) => new(circle);
}

public record CircleUpdate : EntityUpdate<CircleSnapshot>
{
    public CircleUpdate(IEntity entity, Circle circle)
        : base(entity, new CircleSnapshot(circle))
    {
    }
}

/// <summary>
/// Tracks a circle for each participating entity.
/// Entities without an initial circle use <see langword="default"/>.
/// </summary>
public class CircleSystem :
    ISystemWithEntities<ICircleEntity, CircleSnapshot>,
    ISystemAgentContextProvider<CircleSnapshot>,
    ISystemAgentIntentTranslator<Move2DAction>,
    ISystemUpdateAcceptor<CircleSnapshot>,
    ISystemLinkPositionSource,
    ISystemLinkPositionTarget
{
    private readonly Dictionary<IEntity, Circle> _circles = [];

    public IReadOnlyList<IEntity> GetEntities() => [.. _circles.Keys];

    public CircleSnapshot GetTypedState(IEntity entity) => new(_circles[entity]);

    public void InitEntities(params (IEntity entity, CircleSnapshot? initialState)[] initialEntities)
    {
        _circles.Clear();
        foreach (var (entity, initialState) in initialEntities)
            _circles[entity] = initialState ?? default;
    }

    public CircleSnapshot? BuildSnapshot(IAgent agent) =>
        _circles.TryGetValue(agent, out var circle)
            ? new(circle)
            : null;

    public void ApplyUpdates(IEnumerable<EntityUpdate<CircleSnapshot>> updates)
    {
        foreach (var update in updates)
            _circles[update.Entity] = update.State.Circle;
    }

    public UpdateSet TranslateIntent(IAgent agent, Move2DAction action)
    {
        if (!_circles.TryGetValue(agent, out var circle))
        {
            throw new InvalidOperationException(
                $"Agent {agent.Name} does not have a circle in {nameof(CircleSystem)}");
        }

        var moved = circle with { Center = circle.Center + action.Step };
        return new UpdateSet([new CircleUpdate(agent, moved)]);
    }

    Vector2 ISystemLinkPositionSource.GetPosition(IEntity entity)
    {
        // HACK: Return bottom left of circle
        var circle = _circles[entity];
        return new Vector2(circle.Center.X - circle.Radius, circle.Center.Y - circle.Radius);
    }

    void ISystemLinkPositionTarget.SetPosition(IEntity entity, Vector2 position)
    {
        // HACK: assume position is bottom left
        var circle = _circles[entity];
        var newCenter = new Vector2(position.X + circle.Radius, position.Y + circle.Radius);
        _circles[entity] = circle with { Center = newCenter };
    }

}
