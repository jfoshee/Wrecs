using CommerceSim.Core.Spatial;

namespace CommerceSim.Core.Tests;

/// <summary>
/// Represents a property on the Monopoly board with its position and price.
/// </summary>
public record MonopolyProperty(string Name, int Position, int Price);

/// <summary>
/// Standard Monopoly board configuration mapping positions to properties.
/// </summary>
public static class MonopolyBoard
{
    public static readonly List<MonopolyProperty> Properties =
    [
        new("Mediterranean Avenue", 1, 60),
        new("Baltic Avenue", 3, 60),
        new("Oriental Avenue", 6, 100),
        new("Vermont Avenue", 8, 100),
        new("Connecticut Avenue", 9, 120),
        new("St. Charles Place", 11, 140),
        new("States Avenue", 13, 140),
        new("Virginia Avenue", 14, 160),
        new("St. James Place", 16, 180),
        new("Tennessee Avenue", 18, 180),
        new("New York Avenue", 19, 200),
    ];

    public static MonopolyProperty? GetPropertyAtPosition(int position)
        => Properties.FirstOrDefault(p => p.Position == position);
}

/// <summary>
/// Agent responsible for holding initial property inventory and making
/// targeted sell offers to players as they land on properties.
/// </summary>
public class RealEstateAgent(List<MonopolyProperty> boardConfig) : ICommerceAgent, IRequire<TurnSystem>, IRequire<SpatialSystem>
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
        if (currentPlayer is not ICommerceAgent buyer)
            return new DoNothingDecision();

        var playerPosition = _spatialSystem.GetState(currentPlayer).Position;

        // Look up property at that position
        var property = boardConfig.FirstOrDefault(p => p.Position == playerPosition);
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
class MonopolyMovementController(int boardSize) : ISpatialController, IRequire<TurnSystem>, IEntity
{
    private TurnSystem _turnSystem = null!;
    private readonly GameDice _die = new();

    public int Id { get; } = EntityId.Next();
    public string Name => nameof(MonopolyMovementController);

    public IEnumerable<IEntity> GetEntitiesToMove(IEnumerable<IEntity> _)
    {
        return [_turnSystem.GetCurrentPlayer()];
    }

    public int GetNewPosition(IEntity entity, int currentPosition)
    {
        if (entity != _turnSystem.GetCurrentPlayer())
            throw new InvalidOperationException("Only the current player can move");
        int roll = _die.Roll();
        return (currentPosition + roll) % boardSize;
    }

    public void Inject(TurnSystem dependency)
    {
        _turnSystem = dependency;
    }
}

public interface IMonopolyEntity : ISpatialEntity, ITakeTurns, ICommercialEntity;

public record struct MonopolySnapshot() : IStateSnapshot<MonopolySystem>;

public class MonopolySystem : ISystem<IMonopolyEntity, MonopolySnapshot>
{
    const int boardSize = 20;
    private readonly MonopolyMovementController _movementController = new(boardSize);

    public MonopolySnapshot GetState(IEntity entity)
    {
        return default;
    }

    public void InitEntities(params (IEntity entity, MonopolySnapshot? initialState)[] initialEntities)
    {
    }

    public void Tick()
    {
    }
}

public record MonopolyPlayer(string Name) : IMonopolyEntity
{
    public int Id { get; } = EntityId.Next();
}

public class MonopolyGame : Sim
{
    // requires 1 spatial tick = 1 turn = 1 commercial tick
    public MonopolyPlayer Player1 { get; }
    public MonopolyPlayer Player2 { get; }

    public MonopolyGame()
    {
        AddSystem(new TurnSystem());
        AddSystem(new MonopolySystem());
        Player1 = new MonopolyPlayer("Player 1");
        Player2 = new MonopolyPlayer("Player 2");
    }

    public void Init()
    {
        base.InitEntities(
            (new MonopolyMovementController(boardSize: 20), []),
            (Player1, []),
            (Player2, [])
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
}

public class RealEstateAgentTests
{
    // Simple player for testing (implements ICommerceAgent for targeted offers)
    private record TestPlayer(string Name) : ICommerceAgent, ISpatialEntity, ITakeTurns
    {
        public int Id { get; } = EntityId.Next();
        public Decision Decide(CommercialSnapshot state, List<Offer> offers) => new DoNothingDecision();
    }

    [Fact(DisplayName = "Agent makes targeted offer when player lands on owned property")]
    public void MakesTargetedOffer_WhenPlayerOnOwnedProperty()
    {
        // Arrange
        var boardConfig = new List<MonopolyProperty>
        {
            new("Baltic Avenue", 3, 60)
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
        // Arrange
        var boardConfig = new List<MonopolyProperty>
        {
            new("Baltic Avenue", 3, 60)
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
        // Arrange
        var boardConfig = new List<MonopolyProperty>
        {
            new("Baltic Avenue", 3, 60)
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
