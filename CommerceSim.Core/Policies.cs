namespace CommerceSim.Core;

public interface IPolicy
{
    /// <summary>
    /// Called before an accepted offer is executed to determine if it can proceed.
    /// </summary>
    /// <param name="offer">The offer to be evaluated.</param>
    /// <returns>True if the offer can be executed, false otherwise.</returns>
    bool CanExecute(Offer offer);

    /// <summary>
    /// Called after a trade has been executed.
    /// </summary>
    /// <param name="offer">The offer that was executed.</param>
    void OnExecuted(Offer offer);
}

public class OfferSingleUsePolicy : IPolicy
{
    public bool CanExecute(Offer offer) => !offer.Used;

    public void OnExecuted(Offer offer)
    {
        offer.Used = true;
    }
}
