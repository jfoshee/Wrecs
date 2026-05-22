using Wrecs.Core;

namespace Wrecs;

public class Sim
{
    private readonly List<ISystem> _systems = [];
    private readonly List<IEntity> _entities = [];
    private readonly List<IPrepareSharedUpdates> _controllers = [];
    private readonly List<IEvent> _eventQueue = [];
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
        foreach (var system in _systems.OfType<IHasEntities>())
        {
            system.InitEntities(entitiesWithState);
        }
    }

    public void Tick()
    {
        EnsureDependenciesInjected();

        // Preparation Phase
        List<UpdateSet> sharedUpdates = [];
        foreach (var system in _systems.OfType<IPrepareInternalUpdates>())
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

        // Get events to raise
        _eventQueue.Clear();
        var raisers = _systems.OfType<IRaise>()
                              .Concat(_controllers.OfType<IRaise>()); // HACK: controllers can raise events
        foreach (var raiser in raisers)
        {
            var events = raiser.GetEvents();
            _eventQueue.AddRange(events);
        }

        // Raise Events => Call handlers
        var handlers = _systems.OfType<IHandle>();
        foreach (var e in _eventQueue)
        {
            foreach (var handler in handlers)
            {
                handler.Handle(e);
            }
        }

        // HACK: Put all shared updates into one big bucket
        var allUpdates = sharedUpdates.SelectMany(cu => cu.Updates);

        // Update Phase
        foreach (var system in _systems.OfType<IApplyInternalUpdates>())
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

    private void EnsureDependenciesInjected()
    {
        if (_dependenciesInjected)
            return;
        _dependenciesInjected = true;

        var targets = _systems.OfType<IRequire>()
                              .Concat(_entities.OfType<IRequire>()) // TODO: Should we allow injecting into entities? currently required for sources/sinks (flows).
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
