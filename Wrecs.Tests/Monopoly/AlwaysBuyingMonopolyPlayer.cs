namespace Wrecs.Tests.Monopoly;

public record AlwaysBuyingMonopolyPlayer(string Name) : IMonopolyEntity, ICommercialAgent
{
    public int Id { get; } = EntityId.Next();

    public AgentIntent GetIntent(IAgentContext context)
    {
        var opportunities = context.GetSnapshot<OfferListSnapshot>().Offers?.ToList() ?? [];
        var offer = opportunities.OfType<SellOffer>().FirstOrDefault();
        if (offer is null)
            return AgentIntent.Empty;
        // Always accepts offer
        return new(new TakeOfferDecision(offer));
    }
}
