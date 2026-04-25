namespace CommerceSim.Core;

/// <summary>
/// A commercial controller that applies debits from one or more <see cref="ISink"/> instances.
/// </summary>
public class SinksController(IEnumerable<ISink> sinks) : FlowsController<ISink>(
    sinks,
    [new NoNegativeFlowAmountsPolicy(), new NoForcingNegativeBalanceFlowPolicy()])
{
    public SinksController(params ISink[] sinks) : this((IEnumerable<ISink>)sinks) { }
}
