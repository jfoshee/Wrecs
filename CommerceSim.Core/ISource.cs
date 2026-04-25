namespace CommerceSim.Core;

public record struct Grant(IEntity Recipient, int Money, int Resources, string? ResourceType = null);

/// <summary>
/// Creates money or resources and grants them to agents.
/// </summary>
public interface ISource
{
    IEnumerable<Grant> CreateGrants(Context context);
}
