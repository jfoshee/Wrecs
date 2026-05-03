using CommerceSim.Core.Spatial;
using Position = int;

namespace CommerceSim.Core;

public class Sim
{
    private SpatialSystem SpatialSystem => _systems.OfType<SpatialSystem>().First();
    private CommercialSystem CommercialSystem => _systems.OfType<CommercialSystem>().First();
    private OfferSystem OfferSystem => _systems.OfType<OfferSystem>().First();
    private readonly List<ISystem> _systems =
    [
        new SpatialSystem(),
        new CommercialSystem(),
        new OfferSystem(),
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
        foreach (var system in _systems)
        {
            system.InitEntities(entitiesWithState);
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
    public IReadOnlyDictionary<int, CommercialSnapshot> GetStateSnapshot() => CommercialSystem.GetStateSnapshot();
    public IReadOnlyDictionary<int, string> GetAgentNames() => _entities.OfType<ICommercialAgent>().ToDictionary(a => a.Id, a => a.Name);
    public void InitOffers(params Offer[] offers) => OfferSystem.InitOffers(offers);

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
        // We grab each controller only once to make sure we do not call GetEntitiesToUpdate more than once,
        // and we loop over matching systems for each controller. This could be made less awkward.
        var matchingControllers = _controllers
            .Where(controller => IsPrimaryControllerSystem(controller, system))
            .ToArray();

        if (matchingControllers.Length == 0)
            return;

        foreach (var controller in matchingControllers)
        {
            system.ApplyController(controller, GetMatchingSystems(controller));
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

    private bool IsPrimaryControllerSystem(IController controller, ISystem system) =>
        ReferenceEquals(GetMatchingSystems(controller).FirstOrDefault(), system);

    private ISystem[] GetMatchingSystems(IController controller) =>
        [.. _systems.Where(system => system.MatchesController(controller))];
}
