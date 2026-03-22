namespace CommerceSim.Core;

public interface IPolicy
{
    /// <summary>
    /// Called before an accepted offer is executed to determine if it can proceed.
    /// </summary>
    /// <param name="offer">The offer to be evaluated.</param>
    /// <param name="authorState">The state of the agent who created the offer.</param>
    /// <returns>True if the offer can be executed, false otherwise.</returns>
    bool CanExecute(Offer offer, AgentStateSnapshot authorState);

    /// <summary>
    /// Called after a trade has been executed.
    /// </summary>
    /// <param name="offer">The offer that was executed.</param>
    void OnExecuted(Offer offer);
}

public class OfferSingleUsePolicy : IPolicy
{
    public bool CanExecute(Offer offer, AgentStateSnapshot authorState) => !offer.Used;

    public void OnExecuted(Offer offer)
    {
        offer.Used = true;
    }
}

public class CannotCreateResourcesPolicy : IPolicy
{
    public bool CanExecute(Offer offer, AgentStateSnapshot authorState)
    {
        if (offer is SellOffer sellOffer)
        {
            return authorState.ResourceBalance >= sellOffer.Resources;
        }
        return true;
    }

    public void OnExecuted(Offer offer)
    {
        // No state changes needed for this policy after execution
    }
}
