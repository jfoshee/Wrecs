using Wrecs.Core;

namespace Wrecs.Systems.Commercial;

public abstract class Decision : IIntentAction
{
}

public class DoNothingDecision : Decision
{
}

public class TakeOfferDecision(Offer offer) : Decision
{
    public Offer Offer { get; } = offer;
}

public class MakeOfferDecision(Offer offer) : Decision
{
    public Offer Offer { get; } = offer;
}
