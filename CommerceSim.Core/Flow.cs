namespace CommerceSim.Core;

public record struct Flow(IEntity Entity, int Money, int Resources, FlowDirection Direction, string? ResourceType = null)
{
    public readonly int SignedMoney => Direction == FlowDirection.Credit ? Money : -Money;

    public readonly int SignedResources => Direction == FlowDirection.Credit ? Resources : -Resources;

    public static Flow Credit(IEntity recipient, int money, int resources, string? resourceType = null) =>
        new(recipient, money, resources, FlowDirection.Credit, resourceType);

    public static Flow Debit(IEntity payor, int money, int resources, string? resourceType = null) =>
        new(payor, money, resources, FlowDirection.Debit, resourceType);
}

public enum FlowDirection
{
    Credit,
    Debit
}

public interface IFlowOrigin
{
    IEnumerable<Flow> CreateFlows(Context context);
}
