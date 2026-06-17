using Wrecs.Core;

namespace Wrecs.Systems.Commercial;

public static class AgentContextExtensions
{
    public static CommercialSnapshot GetCommercialSnapshot(this IAgentContext context) =>
        new(context.GetSnapshot<MoneySnapshot>(), context.GetSnapshot<InventorySnapshot>());
}
