namespace CommerceSim.Core;

/// <summary>
/// Marker interface for entities in the commercial system (agents, sources, etc.)
/// </summary>
public interface ICommercialEntity : IEntity;

public interface ICommercialController : IController<CommercialSnapshot>;

public class CommercialSystem : ISystem<ICommercialEntity, CommercialSnapshot>, IAcceptUpdates<CommercialSnapshot>
{
    private readonly List<IEntity> _entities = [];
    private readonly Dictionary<IEntity, CommercialState> _states = [];

    public CommercialSnapshot GetState(IEntity entity) => new(_states[entity]);

    public IReadOnlyList<IEntity> GetEntities() => _entities;

    public IReadOnlyDictionary<int, CommercialSnapshot> GetStateSnapshot() =>
        _states.ToDictionary(kvp => kvp.Key.Id, kvp => new CommercialSnapshot(kvp.Value));

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


    public void PrepareUpdates() { }
    public void ApplyUpdates() { }

    public void SetStates(IEnumerable<(IEntity entity, CommercialSnapshot state)> stateUpdates)
    {
        foreach (var (entity, state) in stateUpdates)
        {
            _states[entity] = new CommercialState(state);
        }
    }

    public void ApplyUpdates(IEnumerable<EntityUpdate<CommercialSnapshot>> updates)
    {
        foreach (var update in updates)
        {
            _states[update.Entity] = new(update.State);
        }
    }

    internal class CommercialState
    {
        public int MoneyBalance { get; set; }
        public Dictionary<string, int> Inventory { get; } = [];

        public CommercialState(CommercialSnapshot snapshot)
        {
            MoneyBalance = snapshot.MoneyBalance;
            foreach (var (type, amount) in snapshot.Inventory)
                Inventory[type] = amount;
        }
    }
}
