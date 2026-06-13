using Wrecs.Systems;

namespace Wrecs.Tests;

class DiceMovementController(IGameDice dice) :
    IPrepareSharedUpdates,
    IRequire<TurnSystem>,
    IRequire<Spatial1DSystem>
{
    private TurnSystem? _turnSystem = null!;
    private Spatial1DSystem? _spatial1dSystem = null!;

    public void Inject(TurnSystem dependency) => _turnSystem = dependency;
    public void Inject(Spatial1DSystem dependency) => _spatial1dSystem = dependency;

    public IEnumerable<UpdateSet> PrepareSharedUpdates()
    {
        if (_turnSystem is null)
            throw new InvalidOperationException($"{nameof(TurnSystem)} is required for {nameof(DiceMovementController)}");
        if (_spatial1dSystem is null)
            throw new InvalidOperationException($"{nameof(Spatial1DSystem)} is required for {nameof(DiceMovementController)}");

        // Only move on the first phase of the turn (making no assumptions about how many phases per turn)
        if (_turnSystem.CurrentPhase != 0)
            yield break;

        var currentPlayer = _turnSystem.CurrentPlayer;
        int roll = dice.Roll();
        var currentPosition = _spatial1dSystem.GetTypedState(currentPlayer).Position;
        var newPosition = currentPosition + roll;

        yield return new UpdateSet([new EntityUpdate<Position1DSnapshot>(currentPlayer, new Position1DSnapshot(newPosition))]);
    }
}
