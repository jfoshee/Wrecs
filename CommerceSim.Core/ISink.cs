namespace CommerceSim.Core;

public record struct Charge(IEntity Payor, int Money, int Resources, string? ResourceType = null);

public interface ISink
{
    IEnumerable<Charge> CreateCharges(Context context);
}
