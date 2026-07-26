using Wrecs.Core;

namespace Wrecs;

public class Sim
{
    private readonly List<ISystem> _systems = [];
    private readonly List<ISystem> _disabledSystems = [];
    private readonly List<IEntity> _entities = [];
    private bool _dependenciesInjected = false;

    public void AddSystem(ISystem system)
    {
        _systems.Add(system);
        _dependenciesInjected = false;
    }

    public void AddSystems(params ISystem[] systems)
    {
        _systems.AddRange(systems);
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
            // TODO: Only pass the entities that either: implement the marker interface or have initial state snapshots for the system.
            //       (Currently we handle this using ISystemWithEntities or on a system by system basis)
            system.InitEntities(entitiesWithState);
        }
    }

    public void Tick()
    {
        EnsureDependenciesInjected();

        // Preparation Phase
        List<UpdateSet> proposedUpdates = [];
        foreach (var system in _systems.OfType<ISystemInternalUpdatePreparer>())
        {
            system.PrepareInternalUpdates();
        }
        foreach (var system in _systems.OfType<ISystemSharedUpdates>())
        {
            var updateSets = system.PrepareSharedUpdates();
            proposedUpdates.AddRange(updateSets);
        }

        // Agent invocation phase: Sim builds each agent's context and dispatches intent actions to translators
        foreach (var agent in _entities.OfType<IAgent>())
        {
            var ctx = new AgentContext();
            foreach (var contextProvider in _systems.OfType<ISystemAgentContextProvider>())
                contextProvider.PopulateAgentContext(agent, ctx);
            var intent = agent.GetIntent(ctx);
            if (intent is null)
                continue;
            foreach (var action in intent.Actions)
            {
                // Merge the update sets from all translators so that an intent action becomes a single atomic update set.
                // This prevents systems from getting out of sync if one translator rejects an update set but another approves it.
                var actionUpdates = new List<UpdateSet>();
                foreach (var translator in _systems.OfType<ISystemAgentIntentTranslator>())
                {
                    if (translator.CanTranslate(action))
                        actionUpdates.Add(translator.Translate(agent, action));
                }
                if (actionUpdates.Count > 0)
                {
                    var actionUpdate = MergeUpdateSets(actionUpdates);
                    proposedUpdates.Add(actionUpdate);
                }
            }
        }

        // Enforce constraints: Check each update set
        var eventQueue = new List<IEvent>();
        List<UpdateSet> validUpdates = new(proposedUpdates.Count);
        foreach (var updateSet in proposedUpdates)
        {
            bool rejected = false;
            foreach (var constraint in _systems.OfType<ISystemConstraint>())
            {
                var result = constraint.Validate(updateSet);
                if (!result.IsValid)
                {
                    rejected = true;
                    eventQueue.AddRange(result.Events);
                }
            }
            if (!rejected)
                validUpdates.Add(updateSet);
        }

        // Get events to raise
        var eventRaisers = _systems.OfType<ISystemEventRaiser>();
        foreach (var raiser in eventRaisers)
        {
            var events = raiser.GetEvents();
            eventQueue.AddRange(events);
        }

        // Raise Events => Call handlers
        var eventHandlers = _systems.OfType<ISystemEventHandler>();
        foreach (var e in eventQueue)
        {
            foreach (var handler in eventHandlers)
            {
                handler.Handle(e);
            }
        }

        // HACK: Put all shared updates into one big bucket
        var allUpdates = validUpdates.SelectMany(cu => cu.Updates);

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

    public void DisableSystem<T>() where T : ISystem
    {
        var system = _systems.OfType<T>().SingleOrDefault();
        // If system is already disabled, silently do nothing. This allows for idempotent calls to DisableSystem<T>() without throwing an exception.
        if (system is null)
            return;
        _systems.Remove(system);
        _disabledSystems.Add(system);
    }

    public void EnableSystem<T>() where T : ISystem
    {
        // If system is already enabled, silently do nothing. This allows for idempotent calls to EnableSystem<T>() without throwing an exception.
        if (_systems.OfType<T>().Any())
            return;
        var system = _disabledSystems.OfType<T>().Single();
        _disabledSystems.Remove(system);
        _systems.Add(system);
    }

    private void EnsureDependenciesInjected()
    {
        if (_dependenciesInjected)
            return;
        _dependenciesInjected = true;

        var targets = _systems.OfType<IRequire>()
                              .Concat(_entities.OfType<IRequire>()); // TODO: Should we allow injecting into entities? currently required for Monopoly real estate agent and sources/sinks (flows).

        foreach (var target in targets)
            InjectSystemsIfRequired(target);
    }

    private void InjectSystemsIfRequired(IRequire entity)
    {
        var dependencyContracts = entity.GetType()
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequire<>));

        foreach (var dependencyContract in dependencyContracts)
        {
            var requiredType = dependencyContract.GetGenericArguments()[0];
            // Use IsAssignableFrom so that we can match on interfaces or base classes, not just exact types
            // This allows consumers to be less specific about their dependencies
            var matches = _systems
                .Where(system => requiredType.IsAssignableFrom(system.GetType()))
                .ToList();

            if (matches.Count == 0)
                continue;

            if (matches.Count > 1)
            {
                var matchList = string.Join(", ", matches.Select(s => s.GetType().Name));
                throw new InvalidOperationException(
                    $"Multiple systems match {requiredType.Name} for {entity.GetType().Name}: {matchList}. " +
                    "Register only one matching system or require a more specific type.");
            }

            var injectMethod = dependencyContract.GetMethod(nameof(IRequire<ISystem>.Inject))!;
            injectMethod.Invoke(entity, [matches[0]]);
        }
    }

    private static UpdateSet MergeUpdateSets(List<UpdateSet> actionUpdates)
    {
        var mergedUpdates = new List<IEntityUpdate>();
        foreach (var updateSet in actionUpdates)
        {
            mergedUpdates.AddRange(updateSet.Updates);
        }
        return new UpdateSet(mergedUpdates);
    }
}
