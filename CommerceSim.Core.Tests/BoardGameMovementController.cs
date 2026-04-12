using CommerceSim.Core.Spatial;

namespace CommerceSim.Core.Tests;

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
