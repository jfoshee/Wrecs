namespace Wrecs.Systems.Commercial;

public record class Offer(ICommercialAgent Author, int Price, int Resources, string? ResourceType = null)
{
    public bool Used { get; set; }
}

public record class BuyOffer(ICommercialAgent Buyer, int Price, int Resources, string? ResourceType = null) :
    Offer(Buyer, Price, Resources, ResourceType);

public record class SellOffer(ICommercialAgent Seller, int Price, int Resources, string? ResourceType = null) :
    Offer(Seller, Price, Resources, ResourceType);

/// <summary>
/// An offer to sell that is being made to a specific buyer.
/// </summary>
public record class TargetedSellOffer(ICommercialAgent Seller, ICommercialAgent Buyer, int Price, int Resources, string? ResourceType = null) :
    SellOffer(Seller, Price, Resources, ResourceType);

/// <summary>
/// An offer to buy that is being made to a specific seller.
/// </summary>
public record class TargetedBuyOffer(ICommercialAgent Buyer, ICommercialAgent Seller, int Price, int Resources, string? ResourceType = null) :
    BuyOffer(Buyer, Price, Resources, ResourceType);
