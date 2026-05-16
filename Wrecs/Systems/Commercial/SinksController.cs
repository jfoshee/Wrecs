namespace Wrecs.Systems.Commercial;

public class MoneySinksController(IEnumerable<IMoneySink> sinks) : MoneyFlowsController<IMoneySink>(
    sinks,
    [new NoNegativeMoneyFlowAmountsPolicy(), new NoForcingNegativeMoneyBalanceFlowPolicy()])
{
    public MoneySinksController(params IMoneySink[] sinks) : this((IEnumerable<IMoneySink>)sinks) { }
}

public class ResourceSinksController(IEnumerable<IResourceSink> sinks) : ResourceFlowsController<IResourceSink>(
    sinks,
    [new NoNegativeResourceFlowAmountsPolicy(), new NoForcingNegativeInventoryBalanceFlowPolicy()])
{
    public ResourceSinksController(params IResourceSink[] sinks) : this((IEnumerable<IResourceSink>)sinks) { }
}
