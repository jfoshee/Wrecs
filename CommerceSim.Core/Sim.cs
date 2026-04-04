using CommerceSim.Core.Spatial;
using Position = int;

namespace CommerceSim.Core;

public class Sim
{
    private readonly CommerceSystem _commerceSystem = new();
    private readonly SpatialSystem _spatialSystem = new();
    private readonly List<IEntity> _entities = [];

    public void InitEntities(params (IEntity entity, AgentStateSnapshot? snapshot, int? initialPosition)[] entitiesWithState)
    {
        _entities.Clear();
        // Inject dependencies into all entities and add to master list
        foreach (var (entity, snapshot, initialPosition) in entitiesWithState)
        {
            InitEntity(entity);
            _entities.Add(entity);
        }
        // Init commerce system with entities that have snapshot
        var commercialEntities = entitiesWithState.Where(e => e.snapshot is not null)
            .Select(e => ((ICommerceAgent)e.entity, e.snapshot!.Value))
            .ToArray();
        _commerceSystem.InitAgents(commercialEntities);
        // Init commercial sources
        var sources = entitiesWithState.Select(e => e.entity).OfType<ISource>().ToArray();
        _commerceSystem.InitSources(sources);
        // Init spatial system with entities that have initial position
        var spatialEntities = entitiesWithState.Where(e => e.initialPosition is not null)
            .Select(e => ((ISpatialAgent)e.entity, e.initialPosition!.Value))
            .ToArray();
        _spatialSystem.InitAgents(spatialEntities);
    }

    public void Tick()
    {
        _spatialSystem.Tick();
        _commerceSystem.Tick();
    }

    public AgentStateSnapshot GetAgentState(ICommerceAgent agent) => _commerceSystem.GetState(agent);

    public Position GetPosition(IEntity entity) => _spatialSystem.GetPosition(entity);

    private void InitEntity(IEntity entity)
    {
        if (entity is IRequire<SpatialSystem> spatialEntity)
            spatialEntity.Inject(_spatialSystem);
        if (entity is IRequire<CommerceSystem> commerceEntity)
            commerceEntity.Inject(_commerceSystem);
    }
}
