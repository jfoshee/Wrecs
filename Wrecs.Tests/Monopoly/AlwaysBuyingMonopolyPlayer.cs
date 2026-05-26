namespace Wrecs.Tests.Monopoly;

public record AlwaysBuyingMonopolyPlayer(string Name) : IMonopolyEntity, ICommercialAgent
{
    public int Id { get; } = EntityId.Next();

    public Decision GetIntent(CommercialSnapshot _, List<Offer> opportunities)
    {
        var offer = opportunities.OfType<SellOffer>().FirstOrDefault();
        if (offer is null)
            return new DoNothingDecision();
        // Always accepts offer
        return new TakeOfferDecision(offer);
    }
}
