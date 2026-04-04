namespace CommerceSim.Core;

public record class Offer(ICommerceAgent Author, int Price, int Resources)
{
    public bool Used { get; set; }
}

public record class BuyOffer(ICommerceAgent Buyer, int Price, int Resources) :
    Offer(Buyer, Price, Resources);

public record class SellOffer(ICommerceAgent Seller, int Price, int Resources) :
    Offer(Seller, Price, Resources);
