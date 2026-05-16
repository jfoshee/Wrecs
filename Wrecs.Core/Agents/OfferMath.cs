namespace Wrecs.Core.Agents;

internal static class OfferMath
{
    public static int UnitPrice(Offer offer) => offer.Resources <= 0
        ? int.MaxValue
        : offer.Price / offer.Resources;

    public static MarketSnapshot GetMarketSnapshot(List<Offer> offers, ICommercialAgent self)
    {
        var bestBid = offers
            .OfType<BuyOffer>()
            .Where(x => x.Buyer != self && x.Resources > 0)
            .Select(UnitPrice)
            .DefaultIfEmpty()
            .Max();

        var bestAsk = offers
            .OfType<SellOffer>()
            .Where(x => x.Seller != self && x.Resources > 0)
            .Select(UnitPrice)
            .DefaultIfEmpty(int.MaxValue)
            .Min();

        double? mid = bestBid > 0 && bestAsk < int.MaxValue
            ? (bestBid + bestAsk) / 2.0
            : null;

        return new MarketSnapshot(
            bestBid > 0 ? bestBid : null,
            bestAsk < int.MaxValue ? bestAsk : null,
            mid);
    }

    internal readonly record struct MarketSnapshot(int? BestBid, int? BestAsk, double? MidPrice);
}