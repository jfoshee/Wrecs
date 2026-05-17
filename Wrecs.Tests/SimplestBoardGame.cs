using Wrecs.Systems;

namespace Wrecs.Tests;

class PlayerEntity(string name) : IEntity, ISpatial1DEntity, ITakeTurns
{
    public int Id { get; } = EntityId.Next();
    public string Name => name;
}

class SimplestBoardGame
{
    private readonly Sim _sim = new();

    public PlayerEntity Player1 { get; }
    public PlayerEntity Player2 { get; }

    public SimplestBoardGame(IGameDice? dice = null)
    {
        _sim.AddSystem(new TurnSystem());
        _sim.AddSystem(new Spatial1DSystem());

        dice ??= new GameDice(1);
        var boardGameMovementController = new BoardGameMovementController(dice, boardSize: 10);
        _sim.InitControllers(boardGameMovementController);

        Player1 = new("Player 1");
        Player2 = new("Player 2");
        _sim.InitEntities(
            (Player1, []),
            (Player2, [])
        );
    }

    public void Tick() => _sim.Tick();

    public int GetPosition(IEntity player)
    {
        var spatialSystem = _sim.GetSystem<Spatial1DSystem>();
        return spatialSystem.GetState(player).Position;
    }

    public IEntity GetCurrentPlayer()
    {
        var turnSystem = _sim.GetSystem<TurnSystem>();
        return turnSystem.CurrentPlayer;
    }
}

public class SimplestBoardGameTest
{

    [Fact(DisplayName = "Initialization")]
    public void Initialization()
    {
        var game = new SimplestBoardGame();

        // Assert both players start at position 0
        game.GetPosition(game.Player1).Should().Be(0);
        game.GetPosition(game.Player2).Should().Be(0);
        // Assert it is Player 1's turn
        game.GetCurrentPlayer().Should().Be(game.Player1);
    }

    [Fact(DisplayName = "Single Turn Movement")]
    public void SingleTurnMovement()
    {
        var mockDice = new Mock<IGameDice>();
        mockDice.Setup(d => d.Roll()).Returns(3);
        var game = new SimplestBoardGame(mockDice.Object);

        // Run one tick of the simulation
        game.Tick();

        // Assert Player 1 moved to position 3
        game.GetPosition(game.Player1).Should().Be(3);
        game.GetPosition(game.Player2).Should().Be(0);
        // Assert it is now Player 2's turn
        game.GetCurrentPlayer().Should().Be(game.Player2);
    }
}
