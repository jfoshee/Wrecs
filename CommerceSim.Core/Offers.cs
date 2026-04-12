namespace CommerceSim.Core;

public record class Offer(ICommerceAgent Author, int Price, int Resources)
{
    public bool Used { get; set; }
}

public record class BuyOffer(ICommerceAgent Buyer, int Price, int Resources) :
    Offer(Buyer, Price, Resources);

public record class SellOffer(ICommerceAgent Seller, int Price, int Resources) :
    Offer(Seller, Price, Resources);

/// <summary>
/// An offer to sell that is being made to a specific buyer.
/// </summary>
public record class TargetedSellOffer(ICommerceAgent Seller, ICommerceAgent Buyer, int Price, int Resources) :
    SellOffer(Seller, Price, Resources);
