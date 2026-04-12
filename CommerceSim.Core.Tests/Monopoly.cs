using CommerceSim.Core.Spatial;

namespace CommerceSim.Core.Tests;

class GameDice
{
    private readonly Random _random = new();

    /// <summary>
    /// Rolls a six-sided die and returns a value between 1 and 6.
    /// </summary>
    public int Roll() => _random.Next(1, 7);
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

// Marker
public interface IMonopolyEntity : IEntity, ISpatialEntity, ITakeTurns, ICommercialEntity;

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
