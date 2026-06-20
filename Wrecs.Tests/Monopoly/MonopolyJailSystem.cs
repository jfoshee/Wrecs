using Wrecs.Systems;

namespace Wrecs.Tests.Monopoly;

record struct MonopolyJailSnapshot(bool IsInJail, int TurnsRemaining) : IStateSnapshot<MonopolyJailSystem>;

class MonopolyJailSystem :
    ISystem<IMonopolyEntity, MonopolyJailSnapshot>,
    ISystemWithInternalUpdates,
    ISystemUpdateAcceptor<MonopolyJailSnapshot>,
    IRequire<TurnSystem>
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

    public MonopolyJailSnapshot GetTypedState(IEntity entity)
    {
        if (!_turnsRemaining.TryGetValue(entity, out int value))
            return new MonopolyJailSnapshot(false, 0);
        return new MonopolyJailSnapshot(true, value);
    }

    public void ApplyUpdates(IEnumerable<EntityUpdate<MonopolyJailSnapshot>> updates)
    {
        foreach (var update in updates)
        {
            if (update.State.IsInJail)
                _turnsRemaining[update.Entity] = update.State.TurnsRemaining;
            else
                _turnsRemaining.Remove(update.Entity);
        }
    }

    int? _currentPhase;

    public void PrepareInternalUpdates()
    {
        _currentPhase = _turnSystem?.CurrentPhase;
    }

    public void ApplyInternalUpdates()
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

class MonopolyJailController : ISystemSharedUpdates, IRequire<Spatial1DSystem>
{
    private Spatial1DSystem? _spatial1dSystem;

    public void Inject(Spatial1DSystem dependency) => _spatial1dSystem = dependency;

    public IEnumerable<UpdateSet> PrepareSharedUpdates()
    {
        // Return entities that are on the 30th tile which is the "Go to Jail" space
        foreach (var entity in _spatial1dSystem!.GetEntities().Where(e => _spatial1dSystem.GetTypedState(e) == 30))
        {
            yield return new UpdateSet([
                // Send them to jail for 3 turns
                new EntityUpdate<MonopolyJailSnapshot>(entity, new MonopolyJailSnapshot(true, 3)),
                // Move them to the Jail tile which is at position 10
                new Spatial1DUpdate(entity, 10),
            ]);
        }
    }
}

// TODO: A controller that gets inmates out of jail (e.g. if they possess a PayFineResource)

// Jailer is an agent that makes an offer to allow an inmate to pay $50 to get out of jail.
class JailerAgent : ICommercialAgent, IRequire<MonopolyJailSystem>, IRequire<TurnSystem>
{
    public int Id { get; } = EntityId.Next();
    public string Name => nameof(JailerAgent);

    private MonopolyJailSystem? _jailSystem;
    public void Inject(MonopolyJailSystem dependency) => _jailSystem = dependency;

    private TurnSystem? _turnSystem;
    public void Inject(TurnSystem dependency) => _turnSystem = dependency;

    public AgentIntent GetIntent(IAgentContext context)
    {
        // Make offers to all inmates to pay $50 to get out of jail
        var inmates = _jailSystem?.GetInmates() ?? [];
        foreach (var inmate in inmates)
        {
            // Only make the offer on the inmate's turn (which phase?)
            if (inmate == _turnSystem?.CurrentPlayer && _turnSystem.CurrentPhase == 2)
            {
                // HACK: Do we require that all monopoly players are commercial agents? Maybe we should?
                if (inmate is not ICommercialAgent)
                    continue;
                var inmateAgent = (ICommercialAgent)inmate;
                var offer = new TargetedSellOffer(this, inmateAgent, 50, 1, MonopolyJailSystem.PayFineResource);
                return new(new MakeOfferDecision(offer));
                // yield return offer;
                // TODO: Handle making multiple offers
            }
        }
        return AgentIntent.Empty;
    }
}
