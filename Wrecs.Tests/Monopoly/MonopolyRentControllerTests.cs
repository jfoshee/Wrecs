using Wrecs.Systems;

namespace Wrecs.Tests.Monopoly;

public class MonopolyRentControllerTests
{
    private class TestPlayer(string name) : ICommercialAgent, ISpatial1DEntity, ITakeTurns
    {
        public int Id { get; } = EntityId.Next();
        public string Name { get; } = name;
        public AgentIntent GetIntent(IAgentContext context) => AgentIntent.Empty;
    }

    [Fact(DisplayName = "Player pays rent when landing on property owned by another player")]
    public void PlayerPaysRentWhenLandingOnOwnedProperty()
    {
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

        var spatial1dSystem = new Spatial1DSystem();
        spatial1dSystem.InitEntities(
            (tenant, new Spatial1DSnapshot(3)),   // Tenant on Baltic Avenue
            (landlord, new Spatial1DSnapshot(0))
        );

        var moneySystem = new MoneySystem();
        moneySystem.InitEntities(
            (tenant, new MoneySnapshot(100)),    // Tenant has $100
            (landlord, new MoneySnapshot(50))
        );

        var inventorySystem = new InventorySystem();
        inventorySystem.InitEntities(
            (tenant, null),
            (landlord, new InventorySnapshot([("Baltic Avenue", 1)]))  // Landlord owns Baltic
        );

        var controller = new MonopolyRentController(boardConfig);
        controller.Inject(turnSystem);
        controller.Inject(spatial1dSystem);
        controller.Inject(moneySystem);
        controller.Inject(inventorySystem);

        // Act
        var updateSets = controller.ProposeUpdates().ToList();
        var allUpdates = updateSets.SelectMany(us => us.Updates).OfType<EntityUpdate<MoneySnapshot>>().ToList();
        var tenantUpdate = allUpdates.FirstOrDefault(u => u.Entity == tenant);
        var landlordUpdate = allUpdates.FirstOrDefault(u => u.Entity == landlord);

        // Assert
        updateSets.Should().NotBeEmpty();

        // Rent is 10% of $60 = $6
        tenantUpdate!.State.MoneyBalance.Should().Be(100 - 6);   // Tenant paid $6
        landlordUpdate!.State.MoneyBalance.Should().Be(50 + 6);  // Landlord received $6
    }
}
