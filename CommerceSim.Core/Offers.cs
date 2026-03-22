namespace CommerceSim.Core;

public record class Offer(IAgent Author, int Price, int Resources)
{
    public bool Used { get; set; }
}

public record class BuyOffer(IAgent Buyer, int Price, int Resources) :
    Offer(Buyer, Price, Resources);

public record class SellOffer(IAgent Seller, int Price, int Resources) :
    Offer(Seller, Price, Resources);
