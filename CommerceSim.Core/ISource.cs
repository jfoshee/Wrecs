namespace CommerceSim.Core;

public record struct Grant(IAgent Recipient, int Money, int Resources);

/// <summary>
/// Creates money or resources and grants them to agents.
/// </summary>
public interface ISource
{
    IEnumerable<Grant> CreateGrants(Context context);
}
