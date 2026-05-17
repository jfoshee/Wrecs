using Wrecs.Systems;

namespace Wrecs.Tests;

class PlayerEntity(string name) : IEntity, ISpatial1DEntity, ITakeTurns
{
    public int Id { get; } = EntityId.Next();
    public string Name => name;
}

readonly record struct EndGameSnapshot(bool IsGameOver, bool IsWinner) : IStateSnapshot<SimpleEndGameSystem>;
interface ISimpleEndGamePlayer : IEntity;
class SimpleEndGameSystem : ISystem<ISimpleEndGamePlayer, EndGameSnapshot>
{
    IEntity[] _entities = [];
    private IEntity? _winner;

    public void InitEntities(params (IEntity entity, EndGameSnapshot? initialState)[] initialEntities)
    {
        // TODO: Handle case of starting with a winner / game already over
        _winner = null;
        _entities = [.. initialEntities.Select(e => e.entity)];
    }

    public IReadOnlyList<IEntity> GetEntities() => _entities;

    public EndGameSnapshot GetState(IEntity entity)
    {
        if (_winner == null)
            return new EndGameSnapshot(IsGameOver: false, IsWinner: false);
        return new EndGameSnapshot(IsGameOver: true, IsWinner: entity == _winner);
    }

    public void PrepareInternalUpdates()
    {
    }

    public void ApplyInternalUpdates()
    {
    }

    // TODO: Raise event when game is over
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
        _sim.AddSystem(new SimpleEndGameSystem());

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

    public bool IsGameOver()
    {
        var endGameSystem = _sim.GetSystem<SimpleEndGameSystem>();
        return endGameSystem.GetState(Player1).IsGameOver;
    }

    public bool IsWinner(IEntity player)
    {
        var endGameSystem = _sim.GetSystem<SimpleEndGameSystem>();
        return endGameSystem.GetState(player).IsWinner;
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

    [Fact(DisplayName = "Player 1 Wins Rolling Two Sixes", Skip = "Not implemented yet")]
    public void Player1WinsRollingTwoSixes()
    {
        var mockDice = new Mock<IGameDice>();
        // Player 1 rolls a 6, then a 6 again on their next turn
        mockDice.SetupSequence(d => d.Roll())
            .Returns(6) // Player 1's first turn
            .Returns(1) // Player 2's first turn
            .Returns(6); // Player 1's second turn
        var game = new SimplestBoardGame(mockDice.Object);


        game.Tick(); // Player 1 moves to 6
        game.Tick(); // Player 2 moves to 1

        // Assert neither player has won yet
        game.IsWinner(game.Player1).Should().BeFalse();
        game.IsWinner(game.Player2).Should().BeFalse();
        game.IsGameOver().Should().BeFalse();

        game.Tick(); // Player 1 moves to 12, but board size is 10, so they win

        // Assert Player 1 is back at position 0
        game.GetPosition(game.Player1).Should().Be(0);
        // Assert Player 1 is the victor and the game is over
        game.IsGameOver().Should().BeTrue();
        game.IsWinner(game.Player1).Should().BeTrue();

        // Assert another Tick does not change the state since the game is over
        game.Tick();
        game.GetPosition(game.Player1).Should().Be(0);
        game.GetPosition(game.Player2).Should().Be(1);
        game.GetCurrentPlayer().Should().Be(game.Player1);
        game.IsGameOver().Should().BeTrue();
        game.IsWinner(game.Player1).Should().BeTrue();
    }
}
