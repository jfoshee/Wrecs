using Wrecs.Systems;

namespace Wrecs.Tests.Monopoly;

/// <summary>
/// Agent responsible for holding initial property inventory and making
/// targeted sell offers to players as they land on properties.
/// </summary>
public class RealEstateAgent(MonopolyProperty?[] boardConfig) : ICommercialAgent, IRequire<TurnSystem>, IRequire<Spatial1DSystem>
{
    private TurnSystem _turnSystem = null!;
    private Spatial1DSystem _spatial1dSystem = null!;

    public int Id { get; } = EntityId.Next();
    public string Name => "Real Estate Agent";

    public RealEstateAgent() : this(MonopolyBoard.Properties) { }

    public void Inject(TurnSystem dependency) => _turnSystem = dependency;
    public void Inject(Spatial1DSystem dependency) => _spatial1dSystem = dependency;

    public Decision GetIntent(CommercialSnapshot state, List<Offer> offers)
    {
        // Get current player and their position
        var currentPlayer = _turnSystem.CurrentPlayer;
        if (currentPlayer is not ICommercialAgent buyer)
            return new DoNothingDecision();

        var playerPosition = _spatial1dSystem.GetTypedState(currentPlayer).Position;

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
        // TODO: Offer must "expire" as soon as the next turn.

        return new MakeOfferDecision(offer);
    }
}
