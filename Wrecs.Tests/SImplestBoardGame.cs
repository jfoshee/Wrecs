using Wrecs.Systems;

namespace Wrecs.Tests;

public class SimplestBoardGame
{
    class PlayerEntity(string name) : IEntity, ISpatial1DEntity, ITakeTurns
    {
        public int Id { get; } = EntityId.Next();
        public string Name => name;
    }

    [Fact(DisplayName = "Initialization")]
    public void Initialization()
    {
        var dice = new GameDice(1);
        var boardGameMovementController = new BoardGameMovementController(dice, boardSize: 10);
        var turnSystem = new TurnSystem();
        var player1 = new PlayerEntity("Player 1");
        var player2 = new PlayerEntity("Player 2");
        var sim = new Sim();
        sim.AddSystem(turnSystem);
        sim.InitControllers(boardGameMovementController);
        sim.InitEntities(
            (player1, []),
            (player2, [])
        );

        // Assert both players start at position 0
        var spatialSystem = sim.GetSystem<Spatial1DSystem>();
        spatialSystem.GetState(player1).Position.Should().Be(0);
        spatialSystem.GetState(player2).Position.Should().Be(0);
        // Assert it is Player 1's turn
        turnSystem.GetState(player1).IsMyTurn.Should().BeTrue();
        turnSystem.GetState(player2).IsMyTurn.Should().BeFalse();
    }

    [Fact(DisplayName = "Single Turn Movement")]
    public void SingleTurnMovement()
    {
        var mockDice = new Mock<IGameDice>();
        mockDice.Setup(d => d.Roll()).Returns(3);
        var boardGameMovementController = new BoardGameMovementController(mockDice.Object, boardSize: 10);
        var turnSystem = new TurnSystem();
        var player1 = new PlayerEntity("Player 1");
        var player2 = new PlayerEntity("Player 2");
        var sim = new Sim();
        sim.AddSystem(turnSystem);
        sim.InitControllers(boardGameMovementController);
        sim.InitEntities(
            (player1, []),
            (player2, [])
        );

        // Run one tick of the simulation
        sim.Tick();

        // Assert Player 1 moved to position 3
        var spatialSystem = sim.GetSystem<Spatial1DSystem>();
        spatialSystem.GetState(player1).Position.Should().Be(3);
        spatialSystem.GetState(player2).Position.Should().Be(0);
        // Assert it is now Player 2's turn
        turnSystem.GetState(player1).IsMyTurn.Should().BeFalse();
        turnSystem.GetState(player2).IsMyTurn.Should().BeTrue();
    }
}