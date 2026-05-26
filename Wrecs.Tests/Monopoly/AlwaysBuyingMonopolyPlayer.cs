namespace Wrecs.Tests.Monopoly;

public record AlwaysBuyingMonopolyPlayer(string Name) : IMonopolyEntity, ICommercialAgent
{
    public int Id { get; } = EntityId.Next();

    public Intent GetIntent(CommercialSnapshot _, List<Offer> opportunities)
    {
        var offer = opportunities.OfType<SellOffer>().FirstOrDefault();
        if (offer is null)
            return new(new DoNothingDecision());
        // Always accepts offer
        return new(new TakeOfferDecision(offer));
    }
}
