using System.Reflection;
using CommerceSim.Core.Spatial;
using Position = int;

namespace CommerceSim.Core;

public class Sim
{
    private SpatialSystem SpatialSystem => _systems.OfType<SpatialSystem>().First();
    private CommercialSystem CommercialSystem => _systems.OfType<CommercialSystem>().First();
    private readonly List<ISystem> _systems =
    [
        new SpatialSystem(),
        new CommercialSystem(),
    ];
    private readonly List<IEntity> _entities = [];
    private readonly List<IController> _controllers = [];
    private bool _dependenciesInjected = false;

    public void AddSystem(ISystem system)
    {
        _systems.Add(system);
        _dependenciesInjected = false;
    }

    public void InitControllers(params IController[] controllers)
    {
        _controllers.Clear();
        _controllers.AddRange(controllers);
        _dependenciesInjected = false;
    }

    public void InitEntities(params (IEntity entity, IStateSnapshot[] initialStates)[] entitiesWithState)
    {
        _entities.Clear();
        _dependenciesInjected = false;
        foreach (var (entity, _) in entitiesWithState)
        {
            _entities.Add(entity);
        }

        // Initialize each system with matching entities
        foreach (var system in _systems.Where(ImplementsGeneric))
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
    }

    public void Tick()
    {
        EnsureDependenciesInjected();
        foreach (var system in _systems)
        {
            system.Tick();
            ApplyControllers(system);
        }
    }

    public CommercialSnapshot GetCommercialState(IEntity entity) => CommercialSystem.GetState(entity);
    public Position GetPosition(IEntity entity) => SpatialSystem.GetState(entity).Position;

    private void EnsureDependenciesInjected()
    {
        if (_dependenciesInjected)
            return;
        _dependenciesInjected = true;

        var targets = _systems.OfType<IRequire>()
                              .Concat(_entities.OfType<IRequire>())
                              .Concat(_controllers.OfType<IRequire>());

        foreach (var target in targets)
        {
            foreach (var system in _systems)
                InjectSystemIfRequired(target, system);
        }
    }

    private void ApplyControllers(ISystem system)
    {
        if (!ImplementsGeneric(system))
            return;

        var matchingControllers = _controllers
            .Where(controller => IsPrimaryControllerSystem(controller, system))
            .ToArray();

        if (matchingControllers.Length == 0)
            return;

        foreach (var controller in matchingControllers)
        {
            ApplyController(system, controller);
        }
    }

    private void ApplyController(ISystem system, IController controller)
    {
        var (markerInterface, snapshotType) = GetSystemTypeInfo(system);
        var entities = InvokeGetControllerEntities(system, controller, markerInterface, snapshotType);

        foreach (var targetSystem in GetMatchingSystems(controller))
        {
            var (targetMarkerInterface, targetSnapshotType) = GetSystemTypeInfo(targetSystem);
            InvokeApplyController(targetSystem, controller, entities, targetMarkerInterface, targetSnapshotType);
        }
    }

    #region Ugly Reflection for generic initialization
    /// <summary>
    /// Checks if the system implements ISystem<,> and thus requires generic initialization.
    /// </summary>
    static bool ImplementsGeneric(ISystem system) =>
        system.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISystem<,>));

    private static void InjectSystemIfRequired(IRequire entity, ISystem system)
    {
        var requireInterface = typeof(IRequire<>).MakeGenericType(system.GetType());
        if (requireInterface.IsInstanceOfType(entity))
        {
            var injectMethod = requireInterface.GetMethod(nameof(IRequire<>.Inject))!;
            injectMethod.Invoke(entity, [system]);
        }
    }

    private static bool ImplementsController(IController controller, Type snapshotType) =>
        controller.GetType().GetInterfaces()
            .Any(i => i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(IController<>)
                && i.GetGenericArguments()[0] == snapshotType);

    private bool IsPrimaryControllerSystem(IController controller, ISystem system) =>
        ReferenceEquals(GetMatchingSystems(controller).FirstOrDefault(), system);

    private ISystem[] GetMatchingSystems(IController controller) =>
        [.. _systems.Where(system => ImplementsGeneric(system)
            && ImplementsController(controller, GetSystemTypeInfo(system).snapshotType))];

    private static bool IsSnapshotForSystem(IStateSnapshot snapshot, Type systemType) =>
        snapshot.GetType().GetInterfaces()
            .Any(i => i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(IStateSnapshot<>)
                && i.GetGenericArguments()[0].IsAssignableFrom(systemType));

    /// <summary>
    /// Returns type arguments for ISystem<TMarker, TSnapshot>.
    /// </summary>
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

    private static IEntity[] InvokeGetControllerEntities(ISystem system, IController controller, Type markerInterface, Type snapshotType)
    {
        var method = typeof(Sim).GetMethod(nameof(GetControllerEntitiesCore), BindingFlags.NonPublic | BindingFlags.Static)!;
        var genericMethod = method.MakeGenericMethod(markerInterface, snapshotType);
        return (IEntity[])genericMethod.Invoke(null, [system, controller])!;
    }

    private static void InvokeApplyController(ISystem system, IController controller, IEntity[] entities, Type markerInterface, Type snapshotType)
    {
        var method = typeof(Sim).GetMethod(nameof(ApplyControllerCore), BindingFlags.NonPublic | BindingFlags.Static)!;
        var genericMethod = method.MakeGenericMethod(markerInterface, snapshotType);
        genericMethod.Invoke(null, [system, controller, entities]);
    }

    private static IEntity[] GetControllerEntitiesCore<TMarkerInterface, TStateSnapshot>(ISystem system, IController controller)
        where TMarkerInterface : IEntity
        where TStateSnapshot : struct
    {
        var typedSystem = (ISystem<TMarkerInterface, TStateSnapshot>)system;
        var typedController = (IController<TStateSnapshot>)controller;
        return [.. typedController.GetEntitiesToUpdate(typedSystem.GetEntities())];
    }

    private static void ApplyControllerCore<TMarkerInterface, TStateSnapshot>(ISystem system, IController controller, IEntity[] entities)
        where TMarkerInterface : IEntity
        where TStateSnapshot : struct
    {
        var typedSystem = (ISystem<TMarkerInterface, TStateSnapshot>)system;
        var typedController = (IController<TStateSnapshot>)controller;
        typedSystem.SetStates(entities.Select(entity => (entity, typedController.GetNewState(entity, typedSystem.GetState(entity)))));
    }
    #endregion
}
