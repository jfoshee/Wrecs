namespace CommerceSim.Core.Tests;

/// <summary>
/// Always grants a fixed amount of money and resources to a recipient agent each tick.
/// </summary>
class FixedGrantSource(ICommercialAgent recipient, int money, int resources, string? resourceType = null) : ISource
{
    public IEnumerable<Grant> CreateGrants(Context _)
    {
        yield return new Grant(recipient, money, resources, resourceType);
    }
}
