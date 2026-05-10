using CommerceSim.Core.Spatial;

namespace CommerceSim.Core.Tests.Monopoly;

record struct MonopolyJailSnapshot(bool IsInJail, int TurnsRemaining) : IStateSnapshot<MonopolyJailSystem>;

class MonopolyJailSystem : ISystem<IMonopolyEntity, MonopolyJailSnapshot>, IRequire<TurnSystem>
{
    public const string PayFineResource = "Jail Fine Receipt";
    private readonly List<IEntity> _entities = [];
    public IReadOnlyList<IEntity> GetEntities() => _entities;

    private readonly Dictionary<IEntity, int> _turnsRemaining = [];

    private TurnSystem? _turnSystem;
    public void Inject(TurnSystem dependency) => _turnSystem = dependency;

    public void InitEntities(params (IEntity entity, MonopolyJailSnapshot? initialState)[] initialEntities)
    {
        _entities.Clear();
        foreach (var (entity, initialState) in initialEntities)
        {
            _entities.Add(entity);
            if (initialState.HasValue && initialState.Value.IsInJail)
                _turnsRemaining[entity] = initialState.Value.TurnsRemaining;
        }
    }

    public MonopolyJailSnapshot GetState(IEntity entity)
    {
        if (!_turnsRemaining.TryGetValue(entity, out int value))
            return new MonopolyJailSnapshot(false, 0);
        return new MonopolyJailSnapshot(true, value);
    }

    public void SetStates(IEnumerable<(IEntity entity, MonopolyJailSnapshot state)> stateUpdates)
    {
        foreach (var (entity, state) in stateUpdates)
        {
            if (state.IsInJail)
            {
                _turnsRemaining[entity] = state.TurnsRemaining;
            }
            else
            {
                _turnsRemaining.Remove(entity);
            }
        }
    }

    int? _currentPhase;

    public void PrepareUpdates()
    {
        _currentPhase = _turnSystem?.CurrentPhase;
    }

    public void ApplyUpdates()
    {
        if (_currentPhase != 1)
            return;
        foreach (var entity in _turnsRemaining.Keys.ToList())
        {
            _turnsRemaining[entity]--;
            if (_turnsRemaining[entity] <= 0)
                _turnsRemaining.Remove(entity);
        }
    }

    internal IEnumerable<IEntity> GetInmates() => _turnsRemaining.Keys;
}

class MonopolyJailController :
    IController<MonopolyJailSnapshot>,
    IController<PositionSnapshot>, IRequire<SpatialSystem>
{
    private SpatialSystem? _spatialSystem;

    public IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> allEntities)
    {
        // Return entities that are on the 30th tile which is the "Go to Jail" space
        return allEntities.Where(e => _spatialSystem!.GetState(e) == 30);
    }

    public MonopolyJailSnapshot GetNewState(IEntity entity, MonopolyJailSnapshot currentState)
    {
        // Send them to jail for 3 turns
        return new MonopolyJailSnapshot(true, 3);
    }

    public PositionSnapshot GetNewState(IEntity entity, PositionSnapshot currentState)
    {
        // Move them to the Jail tile which is at position 10
        return new PositionSnapshot(10);
    }

    public void Inject(SpatialSystem dependency) => _spatialSystem = dependency;
}

// TODO: A controller that gets inmates out of jail (e.g. if they possess a PayFineResource)

// Jailer is an agent that makes an offer to allow an inmate to pay $50 to get out of jail.
class JailerAgent : ICommercialAgent, IRequire<MonopolyJailSystem>
{
    public int Id { get; } = EntityId.Next();
    public string Name => nameof(JailerAgent);

    private MonopolyJailSystem? _jailSystem;
    public void Inject(MonopolyJailSystem dependency) => _jailSystem = dependency;

    public Decision Decide(CommercialSnapshot state, List<Offer> offers)
    {
        // TODO: Only make the offer on the inmate's turn (which phase?)
        // Make offers to all inmates to pay $50 to get out of jail
        var inmates = _jailSystem?.GetInmates() ?? [];
        foreach (var inmate in inmates)
        {
            var inmateAgent = (ICommercialAgent)inmate;  // HACK: Do we require that all monopoly players are commercial agents? Maybe we should?
            var offer = new TargetedSellOffer(this, inmateAgent, 50, 1, MonopolyJailSystem.PayFineResource);
            return new MakeOfferDecision(offer);
            // yield return offer;
            // TODO: Handle making multiple offers
        }
        return new DoNothingDecision();
    }
}
