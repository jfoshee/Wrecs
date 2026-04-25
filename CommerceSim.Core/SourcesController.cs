namespace CommerceSim.Core;

/// <summary>
/// A commercial controller that applies credits from one or more <see cref="ISource"/> instances.
/// </summary>
public class SourcesController(IEnumerable<ISource> sources) : FlowsController<ISource>(
    sources,
    [new NoNegativeFlowAmountsPolicy()])
{
    public SourcesController(params ISource[] sources) : this((IEnumerable<ISource>)sources) { }
}
