namespace CommerceSim.Core;

public abstract class FlowsController<TFlowOrigin>(IEnumerable<TFlowOrigin> flowOrigins,
                                                   IEnumerable<IFlowPolicy> flowPolicies) : ICommercialController
    where TFlowOrigin : IFlowOrigin
{
    private readonly List<TFlowOrigin> _flowOrigins = [.. flowOrigins];
    private readonly List<IFlowPolicy> _flowPolicies = [.. flowPolicies];

    private Dictionary<IEntity, List<Flow>> _pendingFlows = [];

    public IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> allEntities)
    {
        var context = new Context(allEntities);
        var allFlows = _flowOrigins.SelectMany(origin => origin.CreateFlows(context));

        _pendingFlows = [];
        foreach (var flow in allFlows)
        {
            var entityFlows = _pendingFlows.GetValueOrDefault(flow.Entity, []);
            entityFlows.Add(flow);
            _pendingFlows[flow.Entity] = entityFlows;
        }

        return _pendingFlows.Keys;
    }

    public CommercialSnapshot GetNewState(IEntity entity, CommercialSnapshot currentState)
    {
        if (!_pendingFlows.TryGetValue(entity, out var flows))
            return currentState;

        int money = currentState.MoneyBalance;
        var inventory = currentState.Inventory.ToDictionary(i => i.Type, i => i.Amount);

        foreach (var flow in flows)
        {
            var inProgress = new CommercialSnapshot(money, inventory);
            if (_flowPolicies.Any(policy => !policy.CanExecute(flow, inProgress)))
                continue;

            money += flow.SignedMoney;
            var key = flow.ResourceType ?? "";
            var current = inventory.GetValueOrDefault(key, 0);
            inventory[key] = current + flow.SignedResources;
        }

        return new CommercialSnapshot(money, inventory);
    }
}