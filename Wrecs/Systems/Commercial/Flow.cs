using Wrecs.Core;

namespace Wrecs.Systems.Commercial;

public record MoneyFlow(IEntity Entity, int Money, FlowDirection Direction)
{
    public int SignedAmount => Direction == FlowDirection.Credit ? Money : -Money;

    public static MoneyFlow Credit(IEntity recipient, int money) =>
        new(recipient, money, FlowDirection.Credit);

    public static MoneyFlow Debit(IEntity payor, int money) =>
        new(payor, money, FlowDirection.Debit);
}

public record ResourceFlow(IEntity Entity, int Resources, FlowDirection Direction, string? ResourceType = null)
{
    public int SignedAmount => Direction == FlowDirection.Credit ? Resources : -Resources;

    public static ResourceFlow Credit(IEntity recipient, int resources, string? resourceType = null) =>
        new(recipient, resources, FlowDirection.Credit, resourceType);

    public static ResourceFlow Debit(IEntity payor, int resources, string? resourceType = null) =>
        new(payor, resources, FlowDirection.Debit, resourceType);
}

public enum FlowDirection
{
    Credit,
    Debit
}

public interface IMoneyFlowOrigin
{
    IEnumerable<MoneyFlow> CreateFlows(FlowContext context);
}

public interface IResourceFlowOrigin
{
    IEnumerable<ResourceFlow> CreateFlows(FlowContext context);
}
