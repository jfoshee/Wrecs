using CommerceSim.Core.Spatial;

namespace CommerceSim.Core.Tests.Monopoly;

public class MonopolyRentControllerTests
{
    private record TestPlayer(string Name) : ICommercialAgent, ISpatialEntity, ITakeTurns
    {
        public int Id { get; } = EntityId.Next();
        public Decision Decide(CommercialSnapshot state, List<Offer> offers) => new DoNothingDecision();
    }

    [Fact(DisplayName = "Player pays rent when landing on property owned by another player")]
    public void PlayerPaysRent_WhenLandingOnOwnedProperty()
    {
        // Arrange - array index = board position
        var boardConfig = new MonopolyProperty?[]
        {
            null, null, null, new("Baltic Avenue", 60)  // Position 3 = Baltic, rent = 6 (10% of price)
        };

        var tenant = new TestPlayer("Tenant");
        var landlord = new TestPlayer("Landlord");

        var turnSystem = new TurnSystem();
        turnSystem.InitEntities(
            (tenant, new TurnSnapshot(IsMyTurn: true)),
            (landlord, null)
        );

        var spatialSystem = new SpatialSystem();
        spatialSystem.InitEntities(
            (tenant, new PositionSnapshot(3)),   // Tenant on Baltic Avenue
            (landlord, new PositionSnapshot(0))
        );

        var commercialSystem = new CommercialSystem();
        commercialSystem.InitEntities(
            (tenant, new CommercialSnapshot(MoneyBalance: 100)),           // Tenant has $100
            (landlord, new CommercialSnapshot(50, [("Baltic Avenue", 1)])) // Landlord owns Baltic, has $50
        );

        var controller = new MonopolyRentController(boardConfig);
        controller.Inject(turnSystem);
        controller.Inject(spatialSystem);
        controller.Inject(commercialSystem);

        // Act
        var entitiesToUpdate = controller.GetEntitiesToUpdate([tenant, landlord]).ToList();

        var tenantNewState = controller.GetNewState(tenant, commercialSystem.GetState(tenant));
        var landlordNewState = controller.GetNewState(landlord, commercialSystem.GetState(landlord));

        // Assert
        entitiesToUpdate.Should().Contain(tenant);
        entitiesToUpdate.Should().Contain(landlord);

        // Rent is 10% of $60 = $6
        tenantNewState.MoneyBalance.Should().Be(100 - 6);   // Tenant paid $6
        landlordNewState.MoneyBalance.Should().Be(50 + 6);  // Landlord received $6
    }
}
