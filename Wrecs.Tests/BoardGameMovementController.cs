using Wrecs.Systems;

namespace Wrecs.Tests;

/// <summary>
/// Raised whenever a player wraps around from the end of the board to the start
/// </summary>
record struct BoardGamePlayerWrappedEvent(IEntity Player) : IEvent;

class BoardGameMovementController(IGameDice dice, int boardSize) :
    IPrepareSharedUpdates,
    IRequire<TurnSystem>,
    IRequire<Spatial1DSystem>,
    IRaise<BoardGamePlayerWrappedEvent>
{
    private TurnSystem? _turnSystem = null!;
    private Spatial1DSystem? _spatial1dSystem = null!;
    private IEntity? _playerWrapped;

    public void Inject(TurnSystem dependency) => _turnSystem = dependency;
    public void Inject(Spatial1DSystem dependency) => _spatial1dSystem = dependency;

    public IEnumerable<UpdateSet> PrepareSharedUpdates()
    {
        if (_turnSystem is null)
            throw new InvalidOperationException($"{nameof(TurnSystem)} is required for {nameof(BoardGameMovementController)}");
        if (_spatial1dSystem is null)
            throw new InvalidOperationException($"{nameof(Spatial1DSystem)} is required for {nameof(BoardGameMovementController)}");

        // Only move on the first phase of the turn (making no assumptions about how many phases per turn)
        if (_turnSystem.CurrentPhase != 0)
            yield break;

        var currentPlayer = _turnSystem.GetCurrentPlayer();
        int roll = dice.Roll();
        var currentPosition = _spatial1dSystem.GetTypedState(currentPlayer).Position;
        var newPosition = currentPosition + roll;
        if (newPosition >= boardSize)
        {
            _playerWrapped = currentPlayer;
            newPosition -= boardSize;
        }
        yield return new UpdateSet([new EntityUpdate<PositionSnapshot>(currentPlayer, new PositionSnapshot(newPosition))]);
    }

    public IEnumerable<BoardGamePlayerWrappedEvent> GetTypedEvents()
    {
        if (_playerWrapped is not null)
        {
            yield return new(_playerWrapped);
            _playerWrapped = null;
        }
    }
}
