namespace CommerceSim.Core;

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

public interface IGrantPolicy
{
    /// <summary>
    /// Called before a grant is executed to determine if it can proceed.
    /// </summary>
    bool CanExecute(Grant grant);
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
        trade.SellerState.ResourceBalance >= trade.Resources;

    public void OnExecuted(Trade trade) { }
}

public class CannotCreateMoneyPolicy : ITradePolicy
{
    public bool CanExecute(Trade trade) =>
        trade.BuyerState.MoneyBalance >= trade.Price;

    public void OnExecuted(Trade trade) { }
}

public class NoNegativeGrantsPolicy : IGrantPolicy
{
    public bool CanExecute(Grant grant) =>
        grant.Money >= 0 && grant.Resources >= 0;
}
