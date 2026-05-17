using Wrecs.Systems;

namespace Wrecs.Tests;

class BoardGameMovementController(IGameDice dice, int boardSize) : IPrepareSharedUpdates, IRequire<TurnSystem>, IRequire<SpatialSystem>
{
    private TurnSystem _turnSystem = null!;
    private SpatialSystem _spatialSystem = null!;
    public void Inject(TurnSystem dependency) => _turnSystem = dependency;
    public void Inject(SpatialSystem dependency) => _spatialSystem = dependency;

    public IEnumerable<UpdateSet> PrepareSharedUpdates()
    {
        // Only move on the first phase of the turn (making no assumptions about how many phases per turn)
        if (_turnSystem.CurrentPhase != 0)
            yield break;

        var currentPlayer = _turnSystem.GetCurrentPlayer();
        int roll = dice.Roll();
        var currentPosition = _spatialSystem.GetState(currentPlayer).Position;
        var newPosition = (currentPosition + roll) % boardSize;
        yield return new UpdateSet([new EntityUpdate<PositionSnapshot>(currentPlayer, new PositionSnapshot(newPosition))]);
    }
}
