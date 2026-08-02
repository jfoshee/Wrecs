using System.Numerics;
using Wrecs.Core;

namespace Wrecs.Systems;

public record struct Spatial2DSnapshot(Vector2 Position) : IStateSnapshot<Spatial2DSystem>
{
    public static implicit operator Vector2(Spatial2DSnapshot snapshot) => snapshot.Position;
    public static implicit operator Spatial2DSnapshot(Vector2 position) => new(position);
}

/// <summary>
/// Marker that an entity has a Spatial2D Position
/// </summary>
public interface ISpatial2DEntity : IEntity;

public record struct Move2DAction(Vector2 Step) : IAgentIntentAction;

public interface ISpatial2DAgent : ISpatial2DEntity, IAgent, IAgentRequireSnapshot<Spatial2DSnapshot>;

public record Spatial2DUpdate : EntityUpdate<Spatial2DSnapshot>
{
    public Spatial2DUpdate(IEntity entity, Vector2 newPosition) : base(entity, new Spatial2DSnapshot(newPosition))
    {
    }
}

public class Spatial2DSystem :
    ISystemWithEntities<ISpatial2DEntity, Spatial2DSnapshot>,
    ISystemAgentContextProvider<Spatial2DSnapshot>,
    ISystemAgentIntentTranslator<Move2DAction>,
    ISystemUpdateAcceptor<Spatial2DSnapshot>,
    ISystemLinkPositionSource,
    ISystemLinkPositionTarget,
    ISpatialSystem
{
    private readonly Dictionary<IEntity, Vector2> _positions = [];

    public Spatial2DSnapshot GetTypedState(IEntity entity) => new(_positions[entity]);

    public IReadOnlyList<IEntity> GetEntities() => [.. _positions.Keys];

    public void InitEntities(params (IEntity entity, Spatial2DSnapshot? initialState)[] initialEntities)
    {
        foreach (var (entity, initialState) in initialEntities)
        {
            _positions[entity] = initialState ?? default;
        }
    }

    public void ApplyUpdates(IEnumerable<EntityUpdate<Spatial2DSnapshot>> updates)
    {
        foreach (var update in updates)
        {
            // NOTE: does not enforce that entity is already present in the system
            _positions[update.Entity] = update.State;
        }
    }


    Vector2 ISystemLinkPositionSource.GetPosition(IEntity entity) => _positions[entity];

    void ISystemLinkPositionTarget.SetPosition(IEntity entity, Vector2 position) => _positions[entity] = position;

    public Spatial2DSnapshot? BuildSnapshot(IAgent agent) =>
        _positions.ContainsKey(agent) ? _positions[agent] : null;

    public UpdateSet TranslateIntent(IAgent agent, Move2DAction action)
    {
        if (!_positions.ContainsKey(agent))
            throw new InvalidOperationException("Agent is not part of Spatial2DSystem");
        var currentPosition = _positions[agent];
        var newPosition = currentPosition + action.Step;
        return new([new Spatial2DUpdate(agent, newPosition)]);
    }

    public float GetDistance(IEntity e1, IEntity e2) =>
        Vector2.Distance(_positions[e1], _positions[e2]);
}
