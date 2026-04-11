using CommerceSim.Core.Spatial;
using Position = int;

namespace CommerceSim.Core;

public class Sim
{
    private readonly CommercialSystem _commercialSystem = new();
    private readonly SpatialSystem _spatialSystem = new();
    private readonly List<IEntity> _entities = [];

    // TODO: Try union types in initialization so can handle various positions and resource types
    // Position could be union of int, double, xy, xyz
    // Resource could be union of int, double (unitless), or resource quantities with units

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
        _commercialSystem.InitAgents(commercialEntities);
        // Init commercial sources
        var sources = entitiesWithState.Select(e => e.entity).OfType<ISource>().ToArray();
        _commercialSystem.InitSources(sources);
        // Init spatial system with entities that are marked as ISpatialEntity or have initial position
        var spatialEntities = entitiesWithState.Where(e => (e.entity is ISpatialEntity) || e.initialPosition is not null)
            .Select(e => (e.entity, e.initialPosition))
            .ToArray();
        _spatialSystem.InitEntities(spatialEntities);
    }

    public void Tick()
    {
        _spatialSystem.Tick();
        _commercialSystem.Tick();
    }

    public AgentStateSnapshot GetAgentState(ICommerceAgent agent) => _commercialSystem.GetState(agent);

    public Position GetPosition(IEntity entity) => _spatialSystem.GetPosition(entity);

    private void InitEntity(IEntity entity)
    {
        if (entity is IRequire<SpatialSystem> spatialEntity)
            spatialEntity.Inject(_spatialSystem);
        if (entity is IRequire<CommercialSystem> commerceEntity)
            commerceEntity.Inject(_commercialSystem);
    }
}
