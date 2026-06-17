using Wrecs.Core;

namespace Wrecs.Systems.Commercial;

public interface IMoneyEntity : IEntity;

public record struct MoneySnapshot(int MoneyBalance) : IStateSnapshot<MoneySystem>;

public class MoneySystem : ISystem<IMoneyEntity, MoneySnapshot>, IAcceptUpdates<MoneySnapshot>, IBuildAgentContext<MoneySnapshot>
{
    private readonly List<IEntity> _entities = [];
    private readonly Dictionary<IEntity, int> _balances = [];

    public MoneySnapshot GetTypedState(IEntity entity) => new(_balances[entity]);

    public IReadOnlyList<IEntity> GetEntities() => _entities;

    public IReadOnlyDictionary<int, MoneySnapshot> GetStateSnapshot() =>
        _balances.ToDictionary(kvp => kvp.Key.Id, kvp => new MoneySnapshot(kvp.Value));

    public void InitEntities(params (IEntity entity, MoneySnapshot? initialState)[] initialEntities)
    {
        _entities.Clear();
        _balances.Clear();
        foreach (var (entity, initialState) in initialEntities)
        {
            _entities.Add(entity);
            _balances[entity] = initialState?.MoneyBalance ?? 0;
        }
    }

    void IHasEntities.InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState)
    {
        var matchingEntities = entitiesWithState
            .Select(e => (e.entity, initialState:
                e.initialStates.OfType<MoneySnapshot>().Select(s => (MoneySnapshot?)s).FirstOrDefault()
                ?? e.initialStates.OfType<CommercialSnapshot>().Select(s => (MoneySnapshot?)s.Money).FirstOrDefault()))
            .Where(e => e.entity is IMoneyEntity || e.initialState != null)
            .ToArray();
        InitEntities(matchingEntities);
    }

    public void PrepareInternalUpdates() { }
    public void ApplyInternalUpdates() { }

    public MoneySnapshot? BuildSnapshot(IAgent agent) =>
        _entities.Contains(agent) ? GetTypedState(agent) : null;

    public void ApplyUpdates(IEnumerable<EntityUpdate<MoneySnapshot>> updates)
    {
        foreach (var update in updates)
        {
            _balances[update.Entity] = update.State.MoneyBalance;
        }
    }
}
