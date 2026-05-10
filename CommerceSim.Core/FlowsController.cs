namespace CommerceSim.Core;

public abstract class FlowsController<TFlowOrigin>(IEnumerable<TFlowOrigin> flowOrigins,
                                                   IEnumerable<IFlowPolicy> flowPolicies)
    : IController<MoneySnapshot>, IController<InventorySnapshot>,
      IRequire<MoneySystem>, IRequire<InventorySystem>
    where TFlowOrigin : IFlowOrigin
{
    private readonly List<TFlowOrigin> _flowOrigins = [.. flowOrigins];
    private readonly List<IFlowPolicy> _flowPolicies = [.. flowPolicies];

    private MoneySystem? _moneySystem;
    private InventorySystem? _inventorySystem;
    private Dictionary<IEntity, List<Flow>> _approvedFlows = [];

    public void Inject(MoneySystem system) => _moneySystem = system;
    public void Inject(InventorySystem system) => _inventorySystem = system;

    public IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> allEntities)
    {
        var context = new Context(allEntities);
        var allFlows = _flowOrigins.SelectMany(origin => origin.CreateFlows(context));

        var pendingFlows = new Dictionary<IEntity, List<Flow>>();
        foreach (var flow in allFlows)
        {
            var entityFlows = pendingFlows.GetValueOrDefault(flow.Entity, []);
            entityFlows.Add(flow);
            pendingFlows[flow.Entity] = entityFlows;
        }

        // Pre-filter by policy using the full combined state so policies that check
        // both money and inventory (e.g. NoForcingNegativeBalanceFlowPolicy) work correctly.
        _approvedFlows = [];
        foreach (var (entity, flows) in pendingFlows)
        {
            var money = (_moneySystem?.GetState(entity) ?? default).MoneyBalance;
            var inventory = (_inventorySystem?.GetState(entity) ?? default).Inventory
                .ToDictionary(i => i.Type, i => i.Amount);

            var approved = new List<Flow>();
            foreach (var flow in flows)
            {
                var inProgress = new CommercialSnapshot(money, inventory.Select(kvp => (kvp.Key, kvp.Value)));
                if (_flowPolicies.Any(policy => !policy.CanExecute(flow, inProgress)))
                    continue;
                approved.Add(flow);
                money += flow.SignedMoney;
                var key = flow.ResourceType ?? "";
                inventory[key] = inventory.GetValueOrDefault(key, 0) + flow.SignedResources;
            }

            if (approved.Count > 0)
                _approvedFlows[entity] = approved;
        }

        return _approvedFlows.Keys;
    }

    public MoneySnapshot GetNewState(IEntity entity, MoneySnapshot current)
    {
        if (!_approvedFlows.TryGetValue(entity, out var flows))
            return current;
        var money = current.MoneyBalance;
        foreach (var flow in flows)
            money += flow.SignedMoney;
        return new MoneySnapshot(money);
    }

    public InventorySnapshot GetNewState(IEntity entity, InventorySnapshot current)
    {
        if (!_approvedFlows.TryGetValue(entity, out var flows))
            return current;
        var inventory = current.Inventory.ToDictionary(i => i.Type, i => i.Amount);
        foreach (var flow in flows)
        {
            var key = flow.ResourceType ?? "";
            inventory[key] = inventory.GetValueOrDefault(key, 0) + flow.SignedResources;
        }
        return new InventorySnapshot(inventory.Select(kvp => (kvp.Key, kvp.Value)));
    }
}
