
namespace CommerceSim.Core.Tests.Monopoly;

public class MonopolyTest(ITestOutputHelper output)
{
    private readonly IOutput _output = new XUnitOutput(output);

    [Fact(DisplayName = "Turns and Movement")]
    public void MonopolyGameTest()
    {
        var game = new MonopolyGame(_output);
        game.Init();

        game.Tick(); // Player 1 moves
        var p1Position = game.GetPosition(game.Player1);
        p1Position.Should().BeInRange(2, 12); // 2 six-sided dice can yield a result between 2 and 12
        game.GetPosition(game.Player2).Should().Be(0);
        game.Tick(); // Resolution phase

        game.Tick(); // Player 2 moves
        game.GetPosition(game.Player1).Should().Be(p1Position); // Player 1 should not have moved
        var p2Position = game.GetPosition(game.Player2);
        p2Position.Should().BeInRange(2, 12);
        game.Tick(); // Resolution phase

        game.Tick(); // Player 1 moves again
        game.GetPosition(game.Player1).Should().BeInRange(p1Position + 2, p1Position + 12); // Player 1 should have moved forward by 2-12 spaces
        game.GetPosition(game.Player2).Should().Be(p2Position); // Player 2 should not have moved
    }

    [Fact(DisplayName = "Player buys Baltic from Real Estate Agent")]
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

    [Fact(DisplayName = "Player 1 buys Baltic, Player 2 lands on it and pays rent")]
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
        game.GetPosition(game.Player1).Should().Be(3);

        game.Tick(); // Agent makes offer, Player 1 buys Baltic
        game.GetCommercialState(game.Player1).GetResourceBalance("Baltic Avenue").Should().Be(1);
        game.GetCommercialState(game.Player1).MoneyBalance.Should().Be(1500 - 60);
        game.GetPosition(game.Player2).Should().Be(0);

        // Player 2 moves to Baltic and pays rent
        game.Tick(); // Player 2 moves to Baltic (position 3)
        game.GetPosition(game.Player2).Should().Be(3);
        game.GetPosition(game.Player1).Should().Be(3);

        // Rent is 10% of property price: 60 / 10 = 6
        game.GetCommercialState(game.Player2).MoneyBalance.Should().Be(1500 - 6);
        game.GetCommercialState(game.Player1).MoneyBalance.Should().Be(1500 - 60 + 6);
    }

    [Fact(DisplayName = "Player 1 lands on Go To Jail after five rolls of 6")]
    public void Player1_LandsOnGoToJail_AfterFiveRollsOfSix()
    {
        var game = new MonopolyGame(_output);
        var mockDice = new Mock<IGameDice>();
        // mockDice.SetupSequence(d => d.Roll())
        mockDice.Setup(d => d.Roll())
            .Returns(6); // Always roll a 6 to ensure we land on Go To Jail after 5 turns
        game.Init(mockDice.Object);

        for (var round = 0; round < 4; round++)
        {
            game.Tick(); // Player 1 moves
            game.Tick(); // Player 1 resolution
            game.Tick(); // Player 2 move, ignored in this scenario
            game.Tick(); // Player 2 resolution
        }
        game.GetPosition(game.Player1).Should().Be(24); // After 4 rounds of rolling 6, Player 1 should be on position 24

        game.Tick(); // Player 1 lands on Go To Jail

        game.GetPosition(game.Player1).Should().Be(10);
        game.GetJailState(game.Player1).IsInJail.Should().BeTrue();
    }

    // TODO: [Fact(DisplayName = "Player 1 rolls doubles three times and goes to jail")]

    // An agent that immediately accepts an offer to buy the right to get out of jail for $50.
    class AlwaysPayingJailFineMonopolyPlayer : IMonopolyEntity, ICommercialAgent
    {
        public int Id { get; } = EntityId.Next();

        public string Name => nameof(AlwaysPayingJailFineMonopolyPlayer);

        public Decision Decide(CommercialSnapshot state, List<Offer> offers)
        {
            var getOutOfJailOffer = offers.FirstOrDefault(offer => offer.ResourceType == MonopolyJailSystem.PayFineResource);
            if (getOutOfJailOffer is not null)
                return new TakeOfferDecision(getOutOfJailOffer);
            return new DoNothingDecision();
        }
    }

    [Fact(DisplayName = "Player 1 gets out of jail by paying $50", Skip = "WIP")]
    public void Player1_GetsOutOfJail_ByPaying50Dollars()
    {
        var game = new MonopolyGame(_output)
        {
            Player1 = new AlwaysPayingJailFineMonopolyPlayer()
        };
        var mockDice = new Mock<IGameDice>();
        mockDice.Setup(d => d.Roll())
            .Returns(6); // Always roll a 6 to ensure we land on Go To Jail after 5 turns
        game.Init(mockDice.Object);

        for (var round = 0; round < 4; round++)
        {
            game.Tick(); // Player 1 moves
            game.Tick(); // Player 1 resolution
            game.Tick(); // Player 2 move, ignored in this scenario
            game.Tick(); // Player 2 resolution
        }
        game.GetPosition(game.Player1).Should().Be(24); // After 4 rounds of rolling 6, Player 1 should be on position 24

        game.Tick(); // Player 1 lands on Go To Jail

        game.GetPosition(game.Player1).Should().Be(10);
        game.GetJailState(game.Player1).IsInJail.Should().BeTrue();

        game.Tick(); // Player 1 pays $50 to get out of jail

        game.GetJailState(game.Player1).IsInJail.Should().BeFalse();
        game.GetCommercialState(game.Player1).MoneyBalance.Should().Be(1500 - 50);
    }
}
