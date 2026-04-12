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

        // Initialize each system with matching entities
        foreach (var system in _systems)
        {
            var (markerInterface, snapshotType) = GetSystemTypeInfo(system);
            // Identify entities that either implement the marker interface
            // or have an initial state matching the system's snapshot type
            var matchingEntities = entitiesWithState
                .Where(e => markerInterface.IsInstanceOfType(e.entity)
                            || e.initialStates.Any(s => IsSnapshotForSystem(s, system.GetType())))
                .Select(e => (e.entity, e.initialStates.FirstOrDefault(s => IsSnapshotForSystem(s, system.GetType()))))
                .ToArray();
            InvokeInitEntities(system, snapshotType, matchingEntities);
        }

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

    #region Ugly Reflection for generic initialization
    private static bool IsSnapshotForSystem(IStateSnapshot snapshot, Type systemType) =>
        snapshot.GetType().GetInterfaces()
            .Any(i => i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(IStateSnapshot<>)
                && i.GetGenericArguments()[0].IsAssignableFrom(systemType));

    private static (Type markerInterface, Type snapshotType) GetSystemTypeInfo(ISystem system) =>
        system.GetType().GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISystem<,>))
            .Select(i => (markerInterface: i.GetGenericArguments()[0], snapshotType: i.GetGenericArguments()[1]))
            .First();

    private static void InvokeInitEntities(ISystem system, Type snapshotType, (IEntity entity, IStateSnapshot? state)[] entities)
    {
        // Build array of (IEntity, TSnapshot?)
        var nullableSnapshotType = typeof(Nullable<>).MakeGenericType(snapshotType);
        var tupleType = typeof(ValueTuple<,>).MakeGenericType(typeof(IEntity), nullableSnapshotType);
        var typedArray = Array.CreateInstance(tupleType, entities.Length);

        for (int i = 0; i < entities.Length; i++)
        {
            var (entity, state) = entities[i];
            var typedTuple = Activator.CreateInstance(tupleType, entity, state);
            typedArray.SetValue(typedTuple, i);
        }

        var method = system.GetType().GetMethod("InitEntities")!;
        method.Invoke(system, [typedArray]);
    }
    #endregion
}
