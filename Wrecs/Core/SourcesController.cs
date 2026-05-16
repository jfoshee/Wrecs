namespace Wrecs.Core;

public class MoneySourcesController(IEnumerable<IMoneySource> sources) : MoneyFlowsController<IMoneySource>(
    sources,
    [new NoNegativeMoneyFlowAmountsPolicy()])
{
    public MoneySourcesController(params IMoneySource[] sources) : this((IEnumerable<IMoneySource>)sources) { }
}

public class ResourceSourcesController(IEnumerable<IResourceSource> sources) : ResourceFlowsController<IResourceSource>(
    sources,
    [new NoNegativeResourceFlowAmountsPolicy()])
{
    public ResourceSourcesController(params IResourceSource[] sources) : this((IEnumerable<IResourceSource>)sources) { }
}
