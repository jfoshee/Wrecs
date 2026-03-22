namespace CommerceSim.Core;

public interface IPolicy
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

public class OfferSingleUsePolicy : IPolicy
{
    public bool CanExecute(Trade trade) =>
        !trade.Offer.Used;

    public void OnExecuted(Trade trade)
    {
        trade.Offer.Used = true;
    }
}

public class CannotCreateResourcesPolicy : IPolicy
{
    public bool CanExecute(Trade trade) =>
        trade.SellerState.ResourceBalance >= trade.Resources;

    public void OnExecuted(Trade trade) { }
}
