namespace CommerceSim.Core.Tests;

/// <summary>
/// Always grants a fixed amount of money and resources to a recipient agent each tick.
/// </summary>
class FixedGrantSource(ICommercialAgent recipient, int money, int resources, string? resourceType = null) : IMoneySource, IResourceSource
{
    IEnumerable<MoneyFlow> IMoneyFlowOrigin.CreateFlows(Context _)
    {
        if (money > 0)
            yield return MoneyFlow.Credit(recipient, money);
    }

    IEnumerable<ResourceFlow> IResourceFlowOrigin.CreateFlows(Context _)
    {
        if (resources > 0)
            yield return ResourceFlow.Credit(recipient, resources, resourceType);
    }
}
