namespace Wrecs.Tests.Sandboxes;

using QState = Sandbox1D1Agent.ExplorerObservation;
using QAction = Sandbox1D1Agent.ExplorerAction;
using QActionRow = Dictionary<Sandbox1D1Agent.ExplorerAction, float>;

class QLearning
{
    // The Q-table is a matrix where rows represent states (S) and columns represent actions (A)
    private readonly Dictionary<QState, QActionRow> _qTable = [];

    public float Q(QState state, QAction action) => 0;
}

public class QLearningTest
{
    [Fact(DisplayName = "Q-Learning Initialization")]
    public void Initialization()
    {
        var subject = new QLearning();

        // Each Q value should start as zero
        // foreach(var state in AllPossibleStates)
        subject.Q(new(default, default, default, default, default, default), QAction.Collect).Should().Be(0);
    }
}