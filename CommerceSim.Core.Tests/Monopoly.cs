using CommerceSim.Core.Spatial;

namespace CommerceSim.Core.Tests;

/// <summary>
/// Represents a property on the Monopoly board with its position and price.
/// </summary>
public record MonopolyProperty(string Name, int Position, int Price);

/// <summary>
/// Standard Monopoly board configuration. Array index = board position.
/// 28 purchasable properties (22 streets + 4 railroads + 2 utilities), 12 nulls.
/// </summary>
public static class MonopolyBoard
{
    public static readonly MonopolyProperty?[] Properties =
    [
        null,                                           // 0: GO
        new("Mediterranean Avenue", 1, 60),             // 1
        null,                                           // 2: Community Chest
        new("Baltic Avenue", 3, 60),                    // 3
        null,                                           // 4: Income Tax
        new("Reading Railroad", 5, 200),                // 5
        new("Oriental Avenue", 6, 100),                 // 6
        null,                                           // 7: Chance
        new("Vermont Avenue", 8, 100),                  // 8
        new("Connecticut Avenue", 9, 120),              // 9
        null,                                           // 10: Jail
        new("St. Charles Place", 11, 140),              // 11
        new("Electric Company", 12, 150),               // 12
        new("States Avenue", 13, 140),                  // 13
        new("Virginia Avenue", 14, 160),                // 14
        new("Pennsylvania Railroad", 15, 200),          // 15
        new("St. James Place", 16, 180),                // 16
        null,                                           // 17: Community Chest
        new("Tennessee Avenue", 18, 180),               // 18
        new("New York Avenue", 19, 200),                // 19
        null,                                           // 20: Free Parking
        new("Kentucky Avenue", 21, 220),                // 21
        null,                                           // 22: Chance
        new("Indiana Avenue", 23, 220),                 // 23
        new("Illinois Avenue", 24, 240),                // 24
        new("B&O Railroad", 25, 200),                   // 25
        new("Atlantic Avenue", 26, 260),                // 26
        new("Ventnor Avenue", 27, 260),                 // 27
        new("Water Works", 28, 150),                    // 28
        new("Marvin Gardens", 29, 280),                 // 29
        null,                                           // 30: Go To Jail
        new("Pacific Avenue", 31, 300),                 // 31
        new("North Carolina Avenue", 32, 300),          // 32
        null,                                           // 33: Community Chest
        new("Pennsylvania Avenue", 34, 320),            // 34
        new("Short Line", 35, 200),                     // 35 (Railroad)
        null,                                           // 36: Chance
        new("Park Place", 37, 350),                     // 37
        null,                                           // 38: Luxury Tax
        new("Boardwalk", 39, 400),                      // 39
    ];

    public static MonopolyProperty? GetPropertyAtPosition(int position)
    {
        if (position < 0 || position >= Properties.Length)
            return null;
        return Properties[position];
    }
}

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

// HACK: Make this an entity just so turn system is injected
class BoardGameMovementController(IGameDice dice, int boardSize) : ISpatialController, IRequire<TurnSystem>, IEntity
{
    private TurnSystem _turnSystem = null!;

    public int Id { get; } = EntityId.Next();
    public string Name => nameof(BoardGameMovementController);

    public IEnumerable<IEntity> GetEntitiesToMove(IEnumerable<IEntity> _)
    {
        return [_turnSystem.GetCurrentPlayer()];
    }

    public int GetNewPosition(IEntity entity, int currentPosition)
    {
        if (entity != _turnSystem.GetCurrentPlayer())
            throw new InvalidOperationException("Only the current player can move");
        int roll = dice.Roll();
        return (currentPosition + roll) % boardSize;
    }

    public void Inject(TurnSystem dependency)
    {
        _turnSystem = dependency;
    }
}

public interface IMonopolyEntity : ISpatialEntity, ITakeTurns, ICommercialEntity;

// public record struct MonopolySnapshot() : IStateSnapshot<MonopolySystem>;

// public class MonopolySystem : ISystem<IMonopolyEntity, MonopolySnapshot>
// {
//     public MonopolySnapshot GetState(IEntity entity)
//     {
//         return default;
//     }

//     public void InitEntities(params (IEntity entity, MonopolySnapshot? initialState)[] initialEntities)
//     {
//     }

//     public void Tick()
//     {
//     }
// }

public record MonopolyPlayer(string Name) : IMonopolyEntity
{
    public int Id { get; } = EntityId.Next();
}

public record AlwaysBuyingMonopolyPlayer(string Name) : IMonopolyEntity, ICommercialAgent
{
    public int Id { get; } = EntityId.Next();

    public Decision Decide(CommercialSnapshot _, List<Offer> opportunities)
    {
        var offer = opportunities.OfType<SellOffer>().FirstOrDefault();
        if (offer is null)
            return new DoNothingDecision();
        // Always accepts offer
        return new TakeOfferDecision(offer);
    }
}

public class MonopolyGame : Sim
{
    // requires 1 spatial tick = 1 turn = 1 commercial tick
    public IMonopolyEntity Player1 { get; internal set; }
    public IMonopolyEntity Player2 { get; }
    public RealEstateAgent RealEstateAgent { get; } = new();

    public MonopolyGame()
    {
        AddSystem(new TurnSystem());
        // AddSystem(new MonopolySystem());
        Player1 = new MonopolyPlayer("Player 1");
        Player2 = new MonopolyPlayer("Player 2");
    }

    public void Init(IGameDice? dice = null)
    {
        dice ??= new GameDice();
        var startingMoney = new CommercialSnapshot(MoneyBalance: 1500, 0);
        var allProperties = new CommercialSnapshot(0, MonopolyBoard.Properties.OfType<MonopolyProperty>().Select(p => (p.Name, 1)));
        InitEntities(
            (new BoardGameMovementController(dice, boardSize: 40), []),
            (RealEstateAgent, [allProperties]),
            (Player1, [startingMoney]),
            (Player2, [startingMoney])
        );
    }
}

public class MonopolyTest
{
    [Fact(DisplayName = "Monopoly Game")]
    public void MonopolyGameTest()
    {
        var game = new MonopolyGame();
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
        var game = new MonopolyGame
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
}

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
            null, null, null, new("Baltic Avenue", 3, 60)  // Position 3 = Baltic
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
            null, null, null, new("Baltic Avenue", 3, 60)  // Position 3 = Baltic
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
            null, null, null, new("Baltic Avenue", 3, 60)  // Position 0 = null, Position 3 = Baltic
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

    [Fact(DisplayName = "Board configuration maps positions to properties correctly")]
    public void BoardConfig_MapsPositionsCorrectly()
    {
        MonopolyBoard.GetPropertyAtPosition(1).Should().NotBeNull();
        MonopolyBoard.GetPropertyAtPosition(1)!.Name.Should().Be("Mediterranean Avenue");

        MonopolyBoard.GetPropertyAtPosition(3).Should().NotBeNull();
        MonopolyBoard.GetPropertyAtPosition(3)!.Name.Should().Be("Baltic Avenue");

        MonopolyBoard.GetPropertyAtPosition(0).Should().BeNull(); // GO space
        MonopolyBoard.GetPropertyAtPosition(10).Should().BeNull(); // Not a property in config
    }
}
