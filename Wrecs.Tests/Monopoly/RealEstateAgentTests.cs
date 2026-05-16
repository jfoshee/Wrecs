using Wrecs.Core.Spatial;

namespace Wrecs.Core.Tests.Monopoly;

public class RealEstateAgentTests
{
    // Simple player for testing (implements ICommercialAgent for targeted offers)
    private record TestPlayer(string Name) : ICommercialAgent, ISpatialEntity, ITakeTurns
    {
        public int Id { get; } = EntityId.Next();
        public Decision Decide(CommercialSnapshot state, List<Offer> offers) => new DoNothingDecision();
    }

    [Fact(DisplayName = "Agent makes targeted offer when player lands on owned property")]
    public void MakesTargetedOffer_WhenPlayerOnOwnedProperty()
    {
        // Arrange - array index = board position
        var boardConfig = new MonopolyProperty?[]
        {
            null, null, null, new("Baltic Avenue", 60)  // Position 3 = Baltic
        };
        var agent = new RealEstateAgent(boardConfig);
        var player = new TestPlayer("Player 1");

        // Set up systems and inject
        var turnSystem = new TurnSystem();
        turnSystem.InitEntities((player, new TurnSnapshot(IsMyTurn: true)));

        var spatialSystem = new SpatialSystem();
        spatialSystem.InitEntities(
            (agent, null),
            (player, new PositionSnapshot(3))   // Player on Baltic Avenue
        );

        agent.Inject(turnSystem);
        agent.Inject(spatialSystem);

        // Agent owns Baltic Avenue (1 unit of "Baltic Avenue" resource type)
        var agentState = new CommercialSnapshot(0, [("Baltic Avenue", 1)]);

        // Act
        var decision = agent.Decide(agentState, []);

        // Assert
        decision.Should().BeOfType<MakeOfferDecision>();
        var offer = ((MakeOfferDecision)decision).Offer;
        offer.Should().BeOfType<TargetedSellOffer>();

        var targetedOffer = (TargetedSellOffer)offer;
        targetedOffer.Buyer.Should().Be(player);
        targetedOffer.Seller.Should().Be(agent);
        targetedOffer.Price.Should().Be(60);
        targetedOffer.Resources.Should().Be(1);
        targetedOffer.ResourceType.Should().Be("Baltic Avenue");
    }

    [Fact(DisplayName = "Agent does not make offer when it doesn't own the property")]
    public void NoOffer_WhenPropertyNotOwned()
    {
        // Arrange - array index = board position
        var boardConfig = new MonopolyProperty?[]
        {
            null, null, null, new("Baltic Avenue", 60)  // Position 3 = Baltic
        };
        var agent = new RealEstateAgent(boardConfig);
        var player = new TestPlayer("Player 1");

        var turnSystem = new TurnSystem();
        turnSystem.InitEntities((player, new TurnSnapshot(true)));

        var spatialSystem = new SpatialSystem();
        spatialSystem.InitEntities(
            (agent, null),
            (player, new PositionSnapshot(3))  // Player on Baltic Avenue
        );

        agent.Inject(turnSystem);
        agent.Inject(spatialSystem);

        // Agent does NOT own Baltic Avenue (empty inventory)
        var agentState = new CommercialSnapshot(0, []);

        // Act
        var decision = agent.Decide(agentState, []);

        // Assert
        decision.Should().BeOfType<DoNothingDecision>();
    }

    [Fact(DisplayName = "Agent does not make offer when player is not on a property")]
    public void NoOffer_WhenNoPropertyAtPosition()
    {
        // Arrange - array index = board position, position 0 is null (GO)
        var boardConfig = new MonopolyProperty?[]
        {
            null, null, null, new("Baltic Avenue", 60)  // Position 0 = null, Position 3 = Baltic
        };
        var agent = new RealEstateAgent(boardConfig);
        var player = new TestPlayer("Player 1");

        var turnSystem = new TurnSystem();
        turnSystem.InitEntities((player, new TurnSnapshot(true)));

        var spatialSystem = new SpatialSystem();
        spatialSystem.InitEntities(
            (agent, null),
            (player, new PositionSnapshot(0))  // Player at GO (position 0, no property)
        );

        agent.Inject(turnSystem);
        agent.Inject(spatialSystem);

        // Agent owns Baltic Avenue but player is not there
        var agentState = new CommercialSnapshot(0, [("Baltic Avenue", 1)]);

        // Act
        var decision = agent.Decide(agentState, []);

        // Assert
        decision.Should().BeOfType<DoNothingDecision>();
    }
}
