using System.Numerics;
using Wrecs.Core;

namespace Wrecs.Systems;

public record struct Velocity2DSnapshot(Vector2 Velocity) : IStateSnapshot<Velocity2DSystem>
{
    public static implicit operator Vector2(Velocity2DSnapshot snapshot) => snapshot.Velocity;
    public static implicit operator Velocity2DSnapshot(Vector2 velocity) => new(velocity);
}

/// <summary>
/// Marker that an entity has a 2D velocity and can be advanced in Spatial2DSystem.
/// </summary>
public interface IVelocity2DEntity : ISpatial2DEntity;

public record Velocity2DUpdate : EntityUpdate<Velocity2DSnapshot>
{
    public Velocity2DUpdate(IEntity entity, Vector2 velocity) : base(entity, new Velocity2DSnapshot(velocity))
    {
    }
}

public class Velocity2DSystem(float dt = 1) :
    ISystemWithEntities<IVelocity2DEntity, Velocity2DSnapshot>,
    ISystemUpdateAcceptor<Velocity2DSnapshot>,
    ISystemUpdateProposer,
    IRequire<Spatial2DSystem>
{
    private readonly Dictionary<IEntity, Vector2> _velocities = [];
    private Spatial2DSystem? _spatial2dSystem;

    public void Inject(Spatial2DSystem dependency) => _spatial2dSystem = dependency;

    public Velocity2DSnapshot GetTypedState(IEntity entity) => new(_velocities[entity]);

    public IReadOnlyList<IEntity> GetEntities() => [.. _velocities.Keys];

    public void InitEntities(params (IEntity entity, Velocity2DSnapshot? initialState)[] initialEntities)
    {
        foreach (var (entity, initialState) in initialEntities)
            _velocities[entity] = initialState ?? default;
    }

    public void ApplyUpdates(IEnumerable<EntityUpdate<Velocity2DSnapshot>> updates)
    {
        foreach (var update in updates)
            _velocities[update.Entity] = update.State;
    }

    public IEnumerable<UpdateSet> ProposeUpdates()
    {
        if (_spatial2dSystem is null)
            throw new InvalidOperationException($"{nameof(Spatial2DSystem)} is required for {nameof(Velocity2DSystem)}");

        foreach (var (entity, velocity) in _velocities)
        {
            var position = _spatial2dSystem.GetTypedState(entity).Position;
            var nextPosition = position + velocity * dt;
            yield return new UpdateSet([new Spatial2DUpdate(entity, nextPosition)]);
        }
    }
}
