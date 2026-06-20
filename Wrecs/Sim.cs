using Wrecs.Core;

namespace Wrecs;

public class Sim
{
    private readonly List<ISystem> _systems = [];
    private readonly List<IEntity> _entities = [];
    private bool _dependenciesInjected = false;

    public void AddSystem(ISystem system)
    {
        _systems.Add(system);
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
        foreach (var system in _systems.OfType<ISystemEntityStateInitializer>())
        {
            system.InitEntities(entitiesWithState);
        }
    }

    public void Tick()
    {
        EnsureDependenciesInjected();

        // Preparation Phase
        List<UpdateSet> sharedUpdates = [];
        foreach (var system in _systems.OfType<ISystemInternalUpdatePreparer>())
        {
            system.PrepareInternalUpdates();
        }
        foreach (var system in _systems.OfType<ISystemSharedUpdates>())
        {
            var updateSet = system.PrepareSharedUpdates();
            sharedUpdates.AddRange(updateSet);
        }

        // Agent invocation phase: Sim builds each agent's context and dispatches intent actions to translators
        // TODO: shuffle agents for fairness (currently processes in registration order)
        foreach (var agent in _entities.OfType<IAgent>())
        {
            var ctx = new AgentContext();
            foreach (var builder in _systems.OfType<ISystemAgentContextBuilder>())
                builder.PopulateAgentContext(agent, ctx);
            var intent = agent.GetIntent(ctx);
            if (intent is null)
                continue;
            foreach (var action in intent.Actions)
            {
                foreach (var translator in _systems.OfType<ISystemAgentIntentTranslator>())
                {
                    if (translator.CanTranslate(action))
                        sharedUpdates.Add(translator.Translate(agent, action));
                }
            }
        }

        // Get events to raise
        var eventQueue = new List<IEvent>();
        var raisers = _systems.OfType<ISystemEventRaiser>();
        foreach (var raiser in raisers)
        {
            var events = raiser.GetEvents();
            eventQueue.AddRange(events);
        }

        // Raise Events => Call handlers
        var handlers = _systems.OfType<ISystemEventHandler>();
        foreach (var e in eventQueue)
        {
            foreach (var handler in handlers)
            {
                handler.Handle(e);
            }
        }

        // HACK: Put all shared updates into one big bucket
        var allUpdates = sharedUpdates.SelectMany(cu => cu.Updates);

        // Update Phase
        foreach (var system in _systems.OfType<ISystemInternalUpdateApplier>())
        {
            system.ApplyInternalUpdates();
        }
        foreach (var system in _systems.OfType<ISystemUpdateAcceptor>())
        {
            system.ApplyUpdates(allUpdates);
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
                              .Concat(_entities.OfType<IRequire>()); // TODO: Should we allow injecting into entities? currently required for Monopoly real estate agent and sources/sinks (flows).

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
