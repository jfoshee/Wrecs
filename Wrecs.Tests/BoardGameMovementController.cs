using Wrecs.Systems;

namespace Wrecs.Tests;

class BoardGameMovementController(IGameDice dice, int boardSize) : ISpatialController, IRequire<TurnSystem>
{
    private TurnSystem _turnSystem = null!;

    public IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> _)
    {
        // Only move on the first phase of the turn (making no assumptions about how many phases per turn)
        if (_turnSystem.CurrentPhase == 0)
            return [_turnSystem.GetCurrentPlayer()];
        return [];
    }

    public PositionSnapshot GetNewState(IEntity entity, PositionSnapshot currentPosition)
    {
        if (entity != _turnSystem.GetCurrentPlayer())
            throw new InvalidOperationException("Only the current player can move");
        int roll = dice.Roll();
        var newPosition = (currentPosition + roll) % boardSize;
        return newPosition;
    }

    public void Inject(TurnSystem dependency)
    {
        _turnSystem = dependency;
    }
}
