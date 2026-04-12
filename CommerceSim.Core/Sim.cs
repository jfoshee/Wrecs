using CommerceSim.Core.Spatial;
using Position = int;

namespace CommerceSim.Core;

public class Sim
{
    private CommercialSystem CommercialSystem => _systems.OfType<CommercialSystem>().First();
    private SpatialSystem SpatialSystem => _systems.OfType<SpatialSystem>().First();
    private readonly List<ISystem> _systems =
    [
        new CommercialSystem(),
        new SpatialSystem()
    ];
    private readonly List<IEntity> _entities = [];

    // TODO: Try union types in initialization so can handle various positions and resource types
    // Position could be union of int, double, xy, xyz
    // Resource could be union of int, double (unitless), or resource quantities with units

    public void InitEntities(params (IEntity entity, IStateSnapshot[] initialStates)[] entitiesWithState)
    {
        _entities.Clear();
        // Inject dependencies into all entities and add to master list
        foreach (var (entity, _) in entitiesWithState)
        {
            InitEntity(entity);
            _entities.Add(entity);
        }

        // TODO: loop over systems
        // Init commerce system with entities that have snapshot
        var commercialEntities = entitiesWithState
            .Where(e => e.entity is ICommercialEntity || e.initialStates.OfType<CommercialSnapshot>().Any())
            .Select(e => (e.entity, e.initialStates.OfType<CommercialSnapshot>().Cast<CommercialSnapshot?>().FirstOrDefault()))
            .ToArray();
        CommercialSystem.InitEntities(commercialEntities);
        // Init spatial system with entities that are marked as ISpatialEntity or have initial position
        var spatialEntities = entitiesWithState
            .Where(e => e.entity is ISpatialEntity || e.initialStates.OfType<PositionSnapshot>().Any())
            .Select(e => (e.entity, e.initialStates.OfType<PositionSnapshot>().Select(p => (int?)p.Position).FirstOrDefault()))
            .ToArray();
        SpatialSystem.InitEntities(spatialEntities);

        // Init commercial sources
        var sources = entitiesWithState.Select(e => e.entity).OfType<ISource>().ToArray();
        CommercialSystem.InitSources(sources);
    }

    public void InitControllers(params ISpatialController[] controllers)
    {
        SpatialSystem.InitControllers(controllers);
    }

    public void Tick()
    {
        SpatialSystem.Tick();
        CommercialSystem.Tick();
    }

    public CommercialSnapshot GetCommercialState(IEntity entity) => CommercialSystem.GetState(entity);
    public Position GetPosition(IEntity entity) => SpatialSystem.GetState(entity).Position;

    private void InitEntity(IEntity entity)
    {
        if (entity is IRequire<SpatialSystem> spatialEntity)
            spatialEntity.Inject(SpatialSystem);
        if (entity is IRequire<CommercialSystem> commerceEntity)
            commerceEntity.Inject(CommercialSystem);
    }
}
