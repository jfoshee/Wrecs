using System.Collections.Immutable;
using Wrecs.Systems;

namespace Wrecs.Tests.Sandboxes;

public enum TicTacToeValue : byte
{
    Empty = 0,
    X = 1,
    O = 2
}

public enum TicTacToeSquare : byte
{
    Empty,
    Mine,
    Opponent
}

public readonly record struct TicTacToeBoardSnapshot : IStateSnapshot
{
    private readonly TicTacToeSquare[,] _squares;

    public TicTacToeBoardSnapshot(TicTacToeValue[,] board, TicTacToeValue player)
    {
        _squares = new TicTacToeSquare[3, 3];
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                _squares[row, col] = board[row, col] switch
                {
                    TicTacToeValue.Empty => TicTacToeSquare.Empty,
                    TicTacToeValue.X when player == TicTacToeValue.X => TicTacToeSquare.Mine,
                    TicTacToeValue.O when player == TicTacToeValue.O => TicTacToeSquare.Mine,
                    _ => TicTacToeSquare.Opponent
                };
            }
        }
    }

    public TicTacToeSquare this[int row, int column] => _squares[row, column];
}

public record struct TicTacToeAction(int Row, int Column) : IAgentIntentAction;

public record struct TicTacToeUpdateSnapshot(int Row, int Column) : IStateSnapshot;

// Keeps track of state of a TicTacToe board
public class TicTacToeBoardSystem :
    ISystemAgentContextProvider<TicTacToeBoardSnapshot>,
    ISystemAgentIntentTranslator<TicTacToeAction>
{
    private readonly TicTacToeValue[,] _board = new TicTacToeValue[3, 3];

    public TicTacToeBoardSnapshot? BuildSnapshot(IAgent agent)
    {
        return new TicTacToeBoardSnapshot(_board, agent.Id == 1 ? TicTacToeValue.X : TicTacToeValue.O);
    }

    public UpdateSet TranslateIntent(IAgent agent, TicTacToeAction action)
    {
        if (_board[action.Row, action.Column] != TicTacToeValue.Empty)
        {
            throw new InvalidOperationException($"Cell ({action.Row}, {action.Column}) is already occupied.");
        }

        // _board[action.Row, action.Column] = agent.Id == 1 ? TicTacToeValue.X : TicTacToeValue.O;

        return new UpdateSet(
        [
            new EntityUpdate<TicTacToeUpdateSnapshot>(agent, new(action.Row, action.Column))
        ]);
    }
}

public class TicTacToeGame
{
    private readonly Sim _sim = new();

    public TicTacToeGame()
    {
        _sim.AddSystems(new TicTacToeBoardSystem(),
                        new TurnSystem());
    }


}
