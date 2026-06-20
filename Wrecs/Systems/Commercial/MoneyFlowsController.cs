using Wrecs.Core;

namespace Wrecs.Systems.Commercial;

public abstract class MoneyFlowsController<TMoneyFlowOrigin>(IEnumerable<TMoneyFlowOrigin> flowOrigins,
                                                             IEnumerable<IMoneyFlowPolicy> flowPolicies)
    : ISystemSharedUpdates, IRequire<MoneySystem>
    where TMoneyFlowOrigin : IMoneyFlowOrigin
{
    private readonly List<TMoneyFlowOrigin> _flowOrigins = [.. flowOrigins];
    private readonly List<IMoneyFlowPolicy> _flowPolicies = [.. flowPolicies];

    protected MoneySystem? _moneySystem;

    public void Inject(MoneySystem system) => _moneySystem = system;

    public IEnumerable<UpdateSet> PrepareSharedUpdates()
    {
        if (_moneySystem == null)
            throw new InvalidOperationException("Dependencies not injected");

        var allEntities = _moneySystem.GetEntities();
        var context = new FlowContext(allEntities);
        var allFlows = _flowOrigins.SelectMany(origin => origin.CreateFlows(context));

        var pendingFlows = new Dictionary<IEntity, List<MoneyFlow>>();
        foreach (var flow in allFlows)
        {
            var entityFlows = pendingFlows.GetValueOrDefault(flow.Entity, []);
            entityFlows.Add(flow);
            pendingFlows[flow.Entity] = entityFlows;
        }

        var updates = new List<IEntityUpdate>();
        foreach (var (entity, flows) in pendingFlows)
        {
            var current = _moneySystem.GetTypedState(entity);
            var money = current.MoneyBalance;

            var approved = new List<MoneyFlow>();
            foreach (var flow in flows)
            {
                var proposed = new MoneySnapshot(money);
                if (_flowPolicies.Any(policy => !policy.CanExecute(flow, proposed)))
                    continue;
                approved.Add(flow);
            }

            if (approved.Count == 0)
                continue;

            foreach (var flow in approved)
                money += flow.SignedAmount;

            updates.Add(new MoneyUpdate(entity, money));
        }

        if (updates.Count > 0)
            yield return new UpdateSet(updates);
    }
}
