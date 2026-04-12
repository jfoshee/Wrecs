namespace CommerceSim.Core;

public record class Offer(ICommerceAgent Author, int Price, int Resources, string? ResourceType = null)
{
    public bool Used { get; set; }
}

public record class BuyOffer(ICommerceAgent Buyer, int Price, int Resources, string? ResourceType = null) :
    Offer(Buyer, Price, Resources, ResourceType);

public record class SellOffer(ICommerceAgent Seller, int Price, int Resources, string? ResourceType = null) :
    Offer(Seller, Price, Resources, ResourceType);

/// <summary>
/// An offer to sell that is being made to a specific buyer.
/// </summary>
public record class TargetedSellOffer(ICommerceAgent Seller, ICommerceAgent Buyer, int Price, int Resources, string? ResourceType = null) :
    SellOffer(Seller, Price, Resources, ResourceType);
