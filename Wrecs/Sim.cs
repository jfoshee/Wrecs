using Wrecs.Core;
using Wrecs.Systems;
using Wrecs.Systems.Commercial;

using Position = int;

namespace Wrecs;

public class Sim
{
    private Spatial1DSystem Spatial1DSystem => _systems.OfType<Spatial1DSystem>().First();
    private readonly List<ISystem> _systems =
    [
        new Spatial1DSystem(),
    ];
    private readonly List<IEntity> _entities = [];
    private readonly List<IPrepareSharedUpdates> _controllers = [];
    private bool _dependenciesInjected = false;

    public void AddSystem(ISystem system)
    {
        _systems.Add(system);
        _dependenciesInjected = false;
    }

    public void InitControllers(params IPrepareSharedUpdates[] controllers)
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
        foreach (var system in _systems)
        {
            system.InitEntities(entitiesWithState);
        }
    }

    public void Tick()
    {
        EnsureDependenciesInjected();

        // Preparation Phase
        List<UpdateSet> sharedUpdates = [];
        foreach (var system in _systems)
        {
            system.PrepareInternalUpdates();
            if (system is IPrepareSharedUpdates sharedUpdateSystem)
            {
                var updateSet = sharedUpdateSystem.PrepareSharedUpdates();
                sharedUpdates.AddRange(updateSet);
            }
        }
        foreach (var controller in _controllers)
        {
            // Each controller is called exactly once per tick; its updates may span multiple systems
            sharedUpdates.AddRange(controller.PrepareSharedUpdates());
        }

        // HACK: Put all shared updates into one big bucket
        var allUpdates = sharedUpdates.SelectMany(cu => cu.Updates);

        // Update Phase
        foreach (var system in _systems)
        {
            system.ApplyInternalUpdates();
            if (system is IAcceptUpdates acceptUpdatesSystem)
            {
                acceptUpdatesSystem.ApplyUpdates(allUpdates);
            }
        }
    }

    public T GetSystem<T>() where T : ISystem =>
        _systems.OfType<T>().Single();

    public CommercialSnapshot GetCommercialState(IEntity entity)
    {
        var moneyState = _systems.OfType<MoneySystem>().First().GetState(entity);
        var inventoryState = _systems.OfType<InventorySystem>().First().GetState(entity);
        return new CommercialSnapshot(moneyState, inventoryState);
    }

    public IReadOnlyDictionary<int, CommercialSnapshot> GetStateSnapshot()
    {
        return _entities.OfType<ICommercialEntity>()
                        .ToDictionary(e => e.Id, e => GetCommercialState(e));
    }

    public Position GetPosition(IEntity entity) => Spatial1DSystem.GetState(entity).Position;
    public IReadOnlyDictionary<int, string> GetAgentNames() => _entities.OfType<ICommercialAgent>().ToDictionary(a => a.Id, a => a.Name);

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

    private static void InjectSystemIfRequired(IRequire entity, ISystem system)
    {
        var requireInterface = typeof(IRequire<>).MakeGenericType(system.GetType());
        if (requireInterface.IsInstanceOfType(entity))
        {
            var injectMethod = requireInterface.GetMethod(nameof(IRequire<>.Inject))!;
            injectMethod.Invoke(entity, [system]);
        }
    }
}
