namespace CommerceSim.Core;

public interface ICommerceAgent : IEntity
{
    /// <summary>
    /// Decide what to do with this tick, given the current state and available offers.
    /// </summary>
    /// <param name="state">The current state of the agent.</param>
    /// <param name="offers">The list of available offers.</param>
    /// <returns>The decision made by the agent.</returns>
    public Decision Decide(AgentStateSnapshot state, List<Offer> offers);
}
