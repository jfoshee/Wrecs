namespace Wrecs.Core;

public abstract class MoneyFlowsController<TMoneyFlowOrigin>(IEnumerable<TMoneyFlowOrigin> flowOrigins,
                                                             IEnumerable<IMoneyFlowPolicy> flowPolicies)
    : IController<MoneySnapshot>, IRequire<MoneySystem>
    where TMoneyFlowOrigin : IMoneyFlowOrigin
{
    private readonly List<TMoneyFlowOrigin> _flowOrigins = [.. flowOrigins];
    private readonly List<IMoneyFlowPolicy> _flowPolicies = [.. flowPolicies];

    protected MoneySystem? _moneySystem;
    private Dictionary<IEntity, List<MoneyFlow>> _approvedFlows = [];

    public void Inject(MoneySystem system) => _moneySystem = system;

    public IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> allEntities)
    {
        if (_moneySystem == null)
            throw new InvalidOperationException("Dependencies not injected");

        var context = new Context(allEntities);
        var allFlows = _flowOrigins.SelectMany(origin => origin.CreateFlows(context));

        var pendingFlows = new Dictionary<IEntity, List<MoneyFlow>>();
        foreach (var flow in allFlows)
        {
            var entityFlows = pendingFlows.GetValueOrDefault(flow.Entity, []);
            entityFlows.Add(flow);
            pendingFlows[flow.Entity] = entityFlows;
        }

        _approvedFlows = [];
        foreach (var (entity, flows) in pendingFlows)
        {
            var money = _moneySystem.GetState(entity).MoneyBalance;

            var approved = new List<MoneyFlow>();
            foreach (var flow in flows)
            {
                var proposed = new MoneySnapshot(money);
                if (_flowPolicies.Any(policy => !policy.CanExecute(flow, proposed)))
                    continue;
                approved.Add(flow);
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
            money += flow.SignedAmount;
        return new MoneySnapshot(money);
    }
}
