using Wrecs.Core;

namespace Wrecs.Systems;

using Position = int;
using Vector = int;

public record struct Spatial1DSnapshot(Position Position) : IStateSnapshot<Spatial1DSystem>
{
    public static implicit operator int(Spatial1DSnapshot snapshot) => snapshot.Position;
    public static implicit operator Spatial1DSnapshot(int position) => new(position);
}

/// <summary>
/// Marker that an entity has a Spatial1D Position
/// </summary>
public interface ISpatial1DEntity : IEntity;

public record struct Move1DAction(Vector Step) : IAgentIntentAction;

public interface ISpatial1DAgent : ISpatial1DEntity, IAgent, IAgentRequireSnapshot<Spatial1DSnapshot>;

public record Spatial1DUpdate : EntityUpdate<Spatial1DSnapshot>
{
    public Spatial1DUpdate(IEntity entity, Position newPosition) : base(entity, new Spatial1DSnapshot(newPosition))
    {
    }
}

public class Spatial1DSystem :
    ISystemWithEntities<ISpatial1DEntity, Spatial1DSnapshot>,
    ISystemAgentContextProvider<Spatial1DSnapshot>,
    ISystemAgentIntentTranslator<Move1DAction>,
    ISystemUpdateAcceptor<Spatial1DSnapshot>,
    ISpatialSystem
{
    private readonly Dictionary<IEntity, Position> _positions = [];

    public Spatial1DSnapshot GetTypedState(IEntity entity) => new(_positions[entity]);

    public IReadOnlyList<IEntity> GetEntities() => [.. _positions.Keys];

    public void InitEntities(params (IEntity entity, Spatial1DSnapshot? initialState)[] initialEntities)
    {
        foreach (var (entity, initialState) in initialEntities)
        {
            _positions[entity] = initialState ?? default;
        }
    }

    public void ApplyUpdates(IEnumerable<EntityUpdate<Spatial1DSnapshot>> updates)
    {
        foreach (var update in updates)
        {
            // NOTE: does not enforce that entity is already present in the system
            _positions[update.Entity] = update.State;
        }
    }

    public Spatial1DSnapshot? BuildSnapshot(IAgent agent) =>
        _positions.ContainsKey(agent) ? _positions[agent] : null;

    public UpdateSet TranslateIntent(IAgent agent, Move1DAction action)
    {
        if (!_positions.ContainsKey(agent))
            throw new InvalidOperationException("Agent is not part of Spatial1DSystem");
        var currentPosition = _positions[agent];
        var newPosition = currentPosition + action.Step;
        return new([new Spatial1DUpdate(agent, newPosition)]);
    }

    public float GetDistance(IEntity e1, IEntity e2) => GetDistance(_positions[e1], _positions[e2]);

    private static float GetDistance(Position p1, Position p2)
    {
        // NOTE: Could overflow for large distances
        return Math.Abs(p1 - p2);
    }
}
