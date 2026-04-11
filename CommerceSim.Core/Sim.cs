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

    public void InitEntities(params (IEntity entity, CommercialSnapshot? initialCommercialState, int? initialPosition)[] entitiesWithState)
    {
        _entities.Clear();
        // Inject dependencies into all entities and add to master list
        foreach (var (entity, initialCommercialState, initialPosition) in entitiesWithState)
        {
            InitEntity(entity);
            _entities.Add(entity);
        }
        // Init commerce system with entities that have snapshot
        var commercialEntities = entitiesWithState.Where(e => e.entity is ICommercialEntity || e.initialCommercialState is not null)
            .Select(e => (e.entity, e.initialCommercialState))
            .ToArray();
        _commercialSystem.InitEntities(commercialEntities);
        // Init commercial sources
        var sources = entitiesWithState.Select(e => e.entity).OfType<ISource>().ToArray();
        _commercialSystem.InitSources(sources);
        // Init spatial system with entities that are marked as ISpatialEntity or have initial position
        var spatialEntities = entitiesWithState.Where(e => (e.entity is ISpatialEntity) || e.initialPosition is not null)
            .Select(e => (e.entity, e.initialPosition))
            .ToArray();
        _spatialSystem.InitEntities(spatialEntities);
    }

    public void InitControllers(params ISpatialController[] controllers)
    {
        _spatialSystem.InitControllers(controllers);
    }

    public void Tick()
    {
        _spatialSystem.Tick();
        _commercialSystem.Tick();
    }

    public CommercialSnapshot GetCommercialState(IEntity entity) => _commercialSystem.GetState(entity);
    public Position GetPosition(IEntity entity) => _spatialSystem.GetState(entity);

    private void InitEntity(IEntity entity)
    {
        if (entity is IRequire<SpatialSystem> spatialEntity)
            spatialEntity.Inject(_spatialSystem);
        if (entity is IRequire<CommercialSystem> commerceEntity)
            commerceEntity.Inject(_commercialSystem);
    }
}
