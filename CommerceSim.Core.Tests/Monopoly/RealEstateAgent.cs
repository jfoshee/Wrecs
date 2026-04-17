using CommerceSim.Core.Spatial;

namespace CommerceSim.Core.Tests.Monopoly;

/// <summary>
/// Agent responsible for holding initial property inventory and making
/// targeted sell offers to players as they land on properties.
/// </summary>
public class RealEstateAgent(MonopolyProperty?[] boardConfig) : ICommercialAgent, IRequire<TurnSystem>, IRequire<SpatialSystem>
{
    private TurnSystem _turnSystem = null!;
    private SpatialSystem _spatialSystem = null!;

    public int Id { get; } = EntityId.Next();
    public string Name => "Real Estate Agent";

    public RealEstateAgent() : this(MonopolyBoard.Properties) { }

    public void Inject(TurnSystem dependency) => _turnSystem = dependency;
    public void Inject(SpatialSystem dependency) => _spatialSystem = dependency;

    public Decision Decide(CommercialSnapshot state, List<Offer> offers)
    {
        // Get current player and their position
        var currentPlayer = _turnSystem.GetCurrentPlayer();
        if (currentPlayer is not ICommercialAgent buyer)
            return new DoNothingDecision();

        var playerPosition = _spatialSystem.GetState(currentPlayer).Position;

        // Look up property at that position (array index = position)
        if (playerPosition < 0 || playerPosition >= boardConfig.Length)
            return new DoNothingDecision();
        var property = boardConfig[playerPosition];
        if (property is null)
            return new DoNothingDecision(); // No property at this position

        // Check if agent owns this property (property name = resource type)
        var ownedAmount = state.GetResourceBalance(property.Name);
        if (ownedAmount <= 0)
            return new DoNothingDecision(); // Don't own this property

        // Make targeted sell offer to the current player
        var offer = new TargetedSellOffer(
            Seller: this,
            Buyer: buyer,
            Price: property.Price,
            Resources: 1,
            ResourceType: property.Name
        );

        return new MakeOfferDecision(offer);
    }
}
