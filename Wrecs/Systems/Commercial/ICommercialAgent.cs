using Wrecs.Core;

namespace Wrecs.Systems.Commercial;

public interface ICommercialAgent : ICommercialEntity
{
    /// <summary>
    /// Decide what to do with this tick, given the current state and available offers.
    /// </summary>
    /// <param name="state">The current state of the agent.</param>
    /// <param name="offers">The list of available offers.</param>
    /// <returns>The decision made by the agent.</returns>
    public Intent GetIntent(CommercialSnapshot state, List<Offer> offers);
}
