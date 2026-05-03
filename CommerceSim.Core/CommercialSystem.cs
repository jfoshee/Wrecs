namespace CommerceSim.Core;

public record struct Trade(Offer Offer,
                           CommercialSnapshot SellerState,
                           CommercialSnapshot BuyerState,
                           int Price,
                           int Resources,
                           string? ResourceType = null);

/// <summary>
/// Marker interface for entities in the commercial system (agents, sources, etc.)
/// </summary>
public interface ICommercialEntity : IEntity
{
}

public interface ICommercialController : IController<CommercialSnapshot>
{
}

public class CommercialSystem : ISystem<ICommercialEntity, CommercialSnapshot>
{
    private readonly List<IEntity> _entities = [];
    private IEnumerable<ICommercialAgent> Agents => _entities.OfType<ICommercialAgent>();
    private readonly Dictionary<IEntity, CommercialState> _states = [];

    public CommercialSnapshot GetState(IEntity entity) => new(_states[entity]);

    public IReadOnlyList<IEntity> GetEntities() => _entities;

    public IReadOnlyDictionary<int, CommercialSnapshot> GetStateSnapshot() =>
        _states.ToDictionary(kvp => kvp.Key.Id, kvp => new CommercialSnapshot(kvp.Value));

    public IReadOnlyDictionary<int, string> GetAgentNames() =>
        Agents.ToDictionary(a => a.Id, a => a.Name);

    public void InitEntities(params (IEntity entity, CommercialSnapshot? initialState)[] initialEntities)
    {
        _entities.Clear();
        _states.Clear();
        foreach (var (entity, initialState) in initialEntities)
        {
            _entities.Add(entity);
            _states[entity] = new(initialState ?? default);
        }
    }

    public void SetStates(IEnumerable<(IEntity entity, CommercialSnapshot state)> stateUpdates)
    {
        foreach (var (entity, state) in stateUpdates)
        {
            _states[entity] = new CommercialState(state);
        }
    }

    public void Tick()
    {
        // Offer processing has moved to OfferSystem
    }

    internal class CommercialState
    {
        // Internally use empty string for unitless (null) since Dictionary doesn't allow null keys
        private const string UnitlessKey = "";

        public int MoneyBalance { get; set; }
        public Dictionary<string, int> Inventory { get; } = [];

        public int GetResourceBalance(string? resourceType) =>
            Inventory.TryGetValue(resourceType ?? UnitlessKey, out var balance) ? balance : 0;

        public void AddResources(string? resourceType, int amount)
        {
            var key = resourceType ?? UnitlessKey;
            if (!Inventory.TryGetValue(key, out var current))
                current = 0;
            Inventory[key] = current + amount;
        }

        public CommercialState(CommercialSnapshot snapshot)
        {
            MoneyBalance = snapshot.MoneyBalance;
            foreach (var (type, amount) in snapshot.Inventory)
                Inventory[type] = amount;
        }
    }
}
