
namespace CommerceSim.Core.Tests.Monopoly;

public class MonopolyTest(ITestOutputHelper output)
{
    private readonly IOutput _output = new XUnitOutput(output);

    [Fact(DisplayName = "Monopoly Game")]
    public void MonopolyGameTest()
    {
        var game = new MonopolyGame(_output);
        game.Init();

        game.Tick(); // Player 1 moves
        game.GetPosition(game.Player1).Should().BeInRange(1, 6);
        game.GetPosition(game.Player2).Should().Be(0);

        game.Tick(); // Player 2 moves
        game.GetPosition(game.Player1).Should().BeInRange(1, 6);
        game.GetPosition(game.Player2).Should().BeInRange(1, 6);

        game.Tick(); // Player 1 moves again
        game.GetPosition(game.Player1).Should().BeInRange(2, 12);
        game.GetPosition(game.Player2).Should().BeInRange(1, 6);
    }

    [Fact(DisplayName = "Player buys Baltic")]
    public void PlayerBuysBaltic()
    {
        // Setup
        var game = new MonopolyGame(_output)
        {
            Player1 = new AlwaysBuyingMonopolyPlayer("Player 1"),
        };
        var mockDice = new Mock<IGameDice>();
        mockDice.Setup(d => d.Roll()).Returns(3); // Always land on Baltic Avenue
        game.Init(mockDice.Object);
        // Starting state: Player 1 has $1500 and 0 properties, RealEstateAgent has all properties including Baltic Avenue
        game.GetCommercialState(game.RealEstateAgent).GetResourceBalance("Baltic Avenue").Should().Be(1);
        game.GetCommercialState(game.Player1).MoneyBalance.Should().Be(1500);
        game.GetCommercialState(game.Player1).GetResourceBalance("Baltic Avenue").Should().Be(0);

        game.Tick(); // Player 1 moves to Baltic
        game.GetPosition(game.Player1).Should().Be(3);
        game.Tick(); // Agent makes offer to sell Baltic, Player 1 takes it

        // Player should have bought Baltic Avenue from the RealEstateAgent
        game.GetCommercialState(game.Player1).GetResourceBalance("Baltic Avenue").Should().Be(1);
        game.GetCommercialState(game.Player1).MoneyBalance.Should().Be(1500 - 60);

        // Real Estate Agent should no longer have Baltic Avenue in inventory
        game.GetCommercialState(game.RealEstateAgent).GetResourceBalance("Baltic Avenue").Should().Be(0);
    }

    [Fact(DisplayName = "Player buys Baltic, next player lands on it and pays rent", Skip = "Not finished")]
    public void PlayerBuysBaltic_NextPlayerPaysRent()
    {
        // Setup
        var game = new MonopolyGame(_output)
        {
            Player1 = new AlwaysBuyingMonopolyPlayer("Player 1"),
        };
        var mockDice = new Mock<IGameDice>();
        mockDice.Setup(d => d.Roll()).Returns(3); // Always land on Baltic Avenue
        game.Init(mockDice.Object);

        // Player 1 moves to Baltic and buys it
        game.Tick(); // Player 1 moves to Baltic
        game.Tick(); // Agent makes offer, Player 1 buys Baltic
        game.GetCommercialState(game.Player1).GetResourceBalance("Baltic Avenue").Should().Be(1);
        game.GetCommercialState(game.Player1).MoneyBalance.Should().Be(1500 - 60);

        // Player 2 moves to Baltic and pays rent
        game.Tick(); // Player 2 moves to Baltic (position 3)
        game.GetPosition(game.Player2).Should().Be(3);

        // Rent is 10% of property price: 60 / 10 = 6
        game.GetCommercialState(game.Player2).MoneyBalance.Should().Be(1500 - 6);
        game.GetCommercialState(game.Player1).MoneyBalance.Should().Be(1500 - 60 + 6);
    }
}
