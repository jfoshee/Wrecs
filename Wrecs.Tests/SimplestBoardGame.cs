using Wrecs.Systems;

namespace Wrecs.Tests;

class PlayerEntity(string name) : IEntity, ISpatial1DEntity, ITakeTurns
{
    public int Id { get; } = EntityId.Next();
    public string Name => name;
}

readonly record struct EndGameSnapshot(bool IsGameOver, bool IsWinner) : IStateSnapshot<SimpleEndGameSystem>;
interface ISimpleEndGamePlayer : IEntity;
struct SimpleEndGameEvent : IEvent;
class SimpleEndGameSystem : ISystem<ISimpleEndGamePlayer, EndGameSnapshot>,
    IRaise<SimpleEndGameEvent>,
    IHandle<BoardGamePlayerWrappedEvent>
{
    IEntity[] _entities = [];
    private IEntity? _winner;
    private bool _gameOverRaised = false;

    public void InitEntities(params (IEntity entity, EndGameSnapshot? initialState)[] initialEntities)
    {
        // TODO: Handle case of starting with a winner / game already over
        _winner = null;
        _gameOverRaised = false;
        _entities = [.. initialEntities.Select(e => e.entity)];
    }

    public IReadOnlyList<IEntity> GetEntities() => _entities;

    public EndGameSnapshot GetTypedState(IEntity entity)
    {
        if (_winner is null)
            return new EndGameSnapshot(IsGameOver: false, IsWinner: false);
        return new EndGameSnapshot(IsGameOver: true, IsWinner: entity == _winner);
    }

    public void PrepareInternalUpdates() { }

    public void ApplyInternalUpdates() { }

    // Raise event when game is over
    public IEnumerable<SimpleEndGameEvent> GetTypedEvents()
    {
        if (_winner is not null && !_gameOverRaised)
        {
            _gameOverRaised = true;
            yield return new();
        }
    }

    public void HandleTyped(BoardGamePlayerWrappedEvent e)
    {
        // The first player to wrap around the board wins
        _winner = e.Player;
    }
}

class SimplestBoardGame
{
    public Sim Sim { get; } = new();

    public PlayerEntity Player1 { get; }
    public PlayerEntity Player2 { get; }

    public SimplestBoardGame(IGameDice? dice = null)
    {
        Sim.AddSystem(new TurnSystem());
        Sim.AddSystem(new Spatial1DSystem());
        Sim.AddSystem(new SimpleEndGameSystem());

        dice ??= new GameDice(1);
        var boardGameMovementController = new BoardGameMovementController(dice, boardSize: 10);
        Sim.AddSystems(boardGameMovementController);

        Player1 = new("Player 1");
        Player2 = new("Player 2");
        Sim.InitEntities(
            (Player1, []),
            (Player2, [])
        );
    }

    public void Tick() => Sim.Tick();

    public int GetPosition(IEntity player)
    {
        var spatialSystem = Sim.GetSystem<Spatial1DSystem>();
        return spatialSystem.GetTypedState(player).Position;
    }

    public IEntity GetCurrentPlayer()
    {
        var turnSystem = Sim.GetSystem<TurnSystem>();
        return turnSystem.CurrentPlayer;
    }

    public bool IsGameOver()
    {
        var endGameSystem = Sim.GetSystem<SimpleEndGameSystem>();
        return endGameSystem.GetTypedState(Player1).IsGameOver;
    }

    public bool IsWinner(IEntity player)
    {
        var endGameSystem = Sim.GetSystem<SimpleEndGameSystem>();
        return endGameSystem.GetTypedState(player).IsWinner;
    }
}

public class SimplestBoardGameTest
{
    class EndGameEventTracker : ISystem, IHandle<SimpleEndGameEvent>
    {
        public int Count { get; private set; }
        public void HandleTyped(SimpleEndGameEvent e) => Count++;

        public void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState) { }
        public void ApplyInternalUpdates() { }
        public void PrepareInternalUpdates() { }
    }

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

    [Fact(DisplayName = "Player 1 Wins Rolling Two Fives")]
    public void Player1WinsRollingTwoFives()
    {
        var mockDice = new Mock<IGameDice>();
        // Player 1 rolls a 5, then a 5 again on their next turn
        mockDice.SetupSequence(d => d.Roll())
            .Returns(5) // Player 1's first turn
            .Returns(1) // Player 2's first turn
            .Returns(5); // Player 1's second turn
        var game = new SimplestBoardGame(mockDice.Object);

        game.Tick(); // Player 1 moves to 5
        game.Tick(); // Player 2 moves to 1

        // Assert neither player has won yet
        game.IsWinner(game.Player1).Should().BeFalse();
        game.IsWinner(game.Player2).Should().BeFalse();
        game.IsGameOver().Should().BeFalse();

        game.Tick(); // Player 1 moves to 10, but board size is 10, so they win

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

    [Fact(DisplayName = "Game Over halts game state changes", Skip = "WIP")]
    public void GameOver()
    {
        var mockDice = new Mock<IGameDice>();
        // Player 1 rolls a 5, then a 5 again on their next turn
        mockDice.SetupSequence(d => d.Roll())
            .Returns(5) // Player 1's first turn
            .Returns(1) // Player 2's first turn
            .Returns(5); // Player 1's second turn
        var game = new SimplestBoardGame(mockDice.Object);

        // Hook a listener to the end game event to verify it is raised exactly once
        var endGameEventTracker = new EndGameEventTracker();
        game.Sim.AddSystem(endGameEventTracker);

        game.Tick(); // Player 1 moves to 5
        game.Tick(); // Player 2 moves to 1
        game.Tick(); // Player 1 moves to 10

        for (int i = 0; i < 5; i++)
        {
            game.Tick();
            game.GetPosition(game.Player1).Should().Be(0);
            game.GetPosition(game.Player2).Should().Be(1);
            game.GetCurrentPlayer().Should().Be(game.Player1);
            game.IsGameOver().Should().BeTrue();
            game.IsWinner(game.Player1).Should().BeTrue();
            endGameEventTracker.Count.Should().Be(1);
        }
    }
}
