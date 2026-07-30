namespace Wrecs.Systems.Commercial;

// TODO: Replace ITradePolicy with ISystemConstraint
[Obsolete("Use ISystemConstraint instead")]
public interface ITradePolicy
{
    /// <summary>
    /// Called before an accepted offer is executed to determine if it can proceed.
    /// </summary>
    /// <param name="offer">The offer to be evaluated.</param>
    /// <param name="authorState">The state of the agent who created the offer.</param>
    /// <param name="counterpartyState">The state of the agent who accepted the offer.</param>
    /// <returns>True if the offer can be executed, false otherwise.</returns>
    bool CanExecute(Trade trade);

    /// <summary>
    /// Called after a trade has been executed.
    /// </summary>
    /// <param name="trade">The trade that was executed.</param>
    void OnExecuted(Trade trade);
}

public interface IMoneyFlowPolicy
{
    /// <summary>
    /// Called before a flow is executed to determine if it can proceed.
    /// </summary>
    bool CanExecute(MoneyFlow flow, MoneySnapshot entityState);
}

public interface IResourceFlowPolicy
{
    /// <summary>
    /// Called before a flow is executed to determine if it can proceed.
    /// </summary>
    bool CanExecute(ResourceFlow flow, InventorySnapshot entityState);
}

public class OfferSingleUsePolicy : ITradePolicy
{
    public bool CanExecute(Trade trade) =>
        !trade.Offer.Used;

    public void OnExecuted(Trade trade)
    {
        trade.Offer.Used = true;
    }
}

public class CannotCreateResourcesPolicy : ITradePolicy
{
    public bool CanExecute(Trade trade) =>
        trade.SellerState.GetResourceBalance(trade.ResourceType) >= trade.Resources;

    public void OnExecuted(Trade trade) { }
}

public class CannotCreateMoneyPolicy : ITradePolicy
{
    public bool CanExecute(Trade trade) =>
        trade.BuyerState.MoneyBalance >= trade.Price;

    public void OnExecuted(Trade trade) { }
}

public class NoNegativeMoneyFlowAmountsPolicy : IMoneyFlowPolicy
{
    public bool CanExecute(MoneyFlow flow, MoneySnapshot _) =>
        flow.Money >= 0;
}

public class NoNegativeResourceFlowAmountsPolicy : IResourceFlowPolicy
{
    public bool CanExecute(ResourceFlow flow, InventorySnapshot _) =>
        flow.Resources >= 0;
}

public class NoForcingNegativeMoneyBalanceFlowPolicy : IMoneyFlowPolicy
{
    public bool CanExecute(MoneyFlow flow, MoneySnapshot entityState) =>
        flow.Direction != FlowDirection.Debit || flow.Money <= entityState.MoneyBalance;
}

public class NoForcingNegativeInventoryBalanceFlowPolicy : IResourceFlowPolicy
{
    public bool CanExecute(ResourceFlow flow, InventorySnapshot entityState) =>
        flow.Direction != FlowDirection.Debit || flow.Resources <= entityState.GetAmount(flow.ResourceType ?? "");
}
