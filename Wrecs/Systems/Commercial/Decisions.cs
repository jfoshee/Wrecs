using Wrecs.Core;

namespace Wrecs.Systems.Commercial;

public abstract class OfferDecision(Offer offer) : IIntentAction
{
    public Offer Offer { get; } = offer;
}

public class TakeOfferDecision(Offer offer) : OfferDecision(offer);

public class MakeOfferDecision(Offer offer) : OfferDecision(offer);
