namespace Wrecs.Systems.Commercial;

public record struct Trade(Offer Offer,
                           CommercialSnapshot SellerState,
                           CommercialSnapshot BuyerState,
                           int Price,
                           int Resources,
                           string? ResourceType = null);
