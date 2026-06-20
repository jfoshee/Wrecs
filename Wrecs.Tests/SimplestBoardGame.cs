using Wrecs.Systems;

namespace Wrecs.Tests;

public class PlayerEntity(string name) : IEntity, ISpatial1DEntity, ITakeTurns
{
    public int Id { get; } = EntityId.Next();
    public string Name => name;
}

readonly record struct EndGameSnapshot(bool IsGameOver, bool IsWinner) : IStateSnapshot<SimpleEndGameSystem>;
interface ISimpleEndGamePlayer : IEntity;
class SimpleEndGameSystem : ISystem<ISimpleEndGamePlayer, EndGameSnapshot>,
    ISystemEventRaiser<EndGameEvent>,
    ISystemEventHandler<WrapAround1DEvent>
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

    // Raise event when game is over
    public IEnumerable<EndGameEvent> GetTypedEvents()
    {
        if (_winner is not null && !_gameOverRaised)
        {
            _gameOverRaised = true;
            yield return new();
        }
    }

    public void HandleTyped(WrapAround1DEvent e)
    {
        // The first player to wrap around the board wins
        _winner = e.Entity;
    }
}

public class SimplestBoardGame
{
    public Sim Sim { get; } = new();

    public PlayerEntity Player1 { get; }
    public PlayerEntity Player2 { get; }
    public int BoardSize { get; }

    public SimplestBoardGame(IGameDice? dice = null, int boardSize = 10)
    {
        BoardSize = boardSize;
        Sim.AddSystem(new TurnSystem());
        Sim.AddSystem(new Spatial1DSystem());
        Sim.AddSystem(new SimpleEndGameSystem());
        dice ??= new GameDice(1);
        Sim.AddSystem(new DiceMovementController(dice));
        Sim.AddSystem(new WrapAroundSystem1D(BoardSize));

        Player1 = new("Player 1");
        Player2 = new("Player 2");
        Sim.InitEntities(
            (Player1, []),
            (Player2, [])
        );
    }

    public void Tick() => Sim.Tick();

    public void Reset()
    {
        Sim.InitEntities(
            (Player1, []),
            (Player2, [])
        );
    }

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

    public string? WinnerName()
    {
        if (!IsGameOver()) return null;
        return IsWinner(Player1) ? Player1.Name : Player2.Name;
    }
}

public class SimplestBoardGameTest
{
    class EndGameEventTracker : ISystem, ISystemEventHandler<EndGameEvent>
    {
        public int Count { get; private set; }
        public void HandleTyped(EndGameEvent e) => Count++;
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
            .Returns(5).Returns(1); // Player 1's second turn, Player 2's second turn
        var game = new SimplestBoardGame(mockDice.Object);

        game.Tick(); // Player 1 moves to 5
        game.Tick(); // Player 2 moves to 1

        // Assert neither player has won yet
        game.IsWinner(game.Player1).Should().BeFalse();
        game.IsWinner(game.Player2).Should().BeFalse();
        game.IsGameOver().Should().BeFalse();

        // Tick 3: Player 1 moves to 10
        game.Tick();

        game.GetPosition(game.Player1).Should().Be(10); // Not wrapped yet

        // Tick 4: WrapAroundSystem1D sees Player 1 at 10, wraps them to 0, raises event. Player 2 moves to 2.
        game.Tick();

        // Assert Player 1 is back at position 0
        game.GetPosition(game.Player1).Should().Be(0);
        game.GetPosition(game.Player2).Should().Be(2);

        // Assert Player 1 is the victor
        game.IsWinner(game.Player1).Should().BeTrue();

        // The game state says it's over, but the event hasn't been raised yet
        game.IsGameOver().Should().BeTrue();

        // Tick 5: EndGameSystem raises EndGameEvent. TurnSystem halts.
        game.Tick();
        game.IsGameOver().Should().BeTrue();

        // Assert another Tick does not change the state since the game is over
        game.Tick();
        game.GetPosition(game.Player1).Should().Be(0);
        // Player 2 is at 2, not 1
        game.GetPosition(game.Player2).Should().Be(2);
        game.GetCurrentPlayer().Should().Be(game.Player1);
        game.IsGameOver().Should().BeTrue();
        game.IsWinner(game.Player1).Should().BeTrue();
    }

    [Fact(DisplayName = "Game Over halts game state changes")]
    public void GameOver()
    {
        var mockDice = new Mock<IGameDice>();
        // Player 1 rolls a 5, then a 5 again on their next turn
        mockDice.SetupSequence(d => d.Roll())
            .Returns(5) // Player 1's first turn
            .Returns(1) // Player 2's first turn
            .Returns(5).Returns(1); // Player 1's second turn, Player 2's second turn
        var game = new SimplestBoardGame(mockDice.Object);

        // Hook a listener to the end game event to verify it is raised exactly once
        var endGameEventTracker = new EndGameEventTracker();
        game.Sim.AddSystem(endGameEventTracker);

        game.Tick(); // P1 moves to 5
        game.Tick(); // P2 moves to 1
        game.Tick(); // P1 moves to 10
        game.Tick(); // Wrap around happens, P1 wins. P2 moves to 2.
        game.Tick(); // EndGameEvent raised.

        for (int i = 0; i < 5; i++)
        {
            game.Tick();
            game.GetPosition(game.Player1).Should().Be(0);
            game.GetPosition(game.Player2).Should().Be(2);
            game.GetCurrentPlayer().Should().Be(game.Player1);
            game.IsGameOver().Should().BeTrue();
            game.IsWinner(game.Player1).Should().BeTrue();
            endGameEventTracker.Count.Should().Be(1);
        }
    }
}
