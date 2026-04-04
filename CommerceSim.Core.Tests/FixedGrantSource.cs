namespace CommerceSim.Core.Tests;

/// <summary>
/// Always grants a fixed amount of money and resources to a recipient agent each tick.
/// </summary>
class FixedGrantSource(IAgent recipient, int money, int resources) : ISource
{
    public IEnumerable<Grant> CreateGrants()
    {
        yield return new Grant(recipient, money, resources);
    }
}
