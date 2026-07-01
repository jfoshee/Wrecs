namespace Wrecs.Tests.Sandboxes;

using QAction = Sandbox1D1Agent.ExplorerAction;
using QActionRow = Dictionary<Sandbox1D1Agent.ExplorerAction, float>;
using QState = Sandbox1D1Agent.ExplorerObservation;

class QLearning
{
    // The Q-table is a matrix where rows represent states (S) and columns represent actions (A)
    private readonly Dictionary<QState, QActionRow> _q = [];

    public float Q(QState state, QAction action) => 0;

    internal void SetQ(QState state, QAction action, float value)
    {
        if (!_q.ContainsKey(state))
        {
            // _q[state] = [[action, value]];
            _q[state] = [];
        }
        _q[state][action] = value;
    }

    private static QAction MaxQ(QActionRow row) => Enumerable.MaxBy(row, cell => cell.Value).Key;

    public QAction ChooseAction(QState state)
    {
        if (_q.TryGetValue(state, out QActionRow? row))
        {
            return MaxQ(row);
        }
        // TODO: random value?
        return default;
    }
}

public class QLearningTest
{
    static IEnumerable<QState> AllStates()
    {
        int[] balances = [-100, -10, -1, 0, 1, 10, 100];
        bool[] bools = [false, true];
        foreach (var resourceBalance in balances)
            foreach (var moneyBalance in balances)
                foreach (var canCollect in bools)
                    foreach (var canSell in bools)
                        foreach (var sourceVisible in bools)
                            foreach (var buyerVisible in bools)
                                yield return new(resourceBalance, moneyBalance, canCollect, canSell, sourceVisible, buyerVisible);
    }

    static IEnumerable<QAction> AllActions => [QAction.Collect, QAction.MoveLeft, QAction.MoveRight, QAction.Sell, QAction.Stay];

    [Fact(DisplayName = "Q-Learning Initialization")]
    public void Initialization()
    {
        var subject = new QLearning();

        // Each Q value should start as zero
        foreach (var state in AllStates())
            foreach (var action in AllActions)
                subject.Q(state, action).Should().Be(0);
    }

    [Fact(DisplayName = "Choose max value Action for given State")]
    public void ChooseAction()
    {
        var subject = new QLearning();
        var state = new QState(42, 24, true, false, true, false);

        // Setup fake Q values
        subject.SetQ(state, QAction.Stay, 0.2f);
        subject.SetQ(state, QAction.MoveLeft, 0.3f);
        subject.SetQ(state, QAction.MoveRight, 0.4f);
        subject.SetQ(state, QAction.Collect, 0.9f);
        subject.SetQ(state, QAction.Sell, 0.7f);

        subject.ChooseAction(state).Should().Be(QAction.Collect);
    }

    // The table is updated using the Bellman Equation after every step the agent takes:
    // \(Q(s,a)\leftarrow Q(s,a)+\alpha \left[r+\gamma \max _{a^{\prime }}Q(s^{\prime },a^{\prime })-Q(s,a)\right]\)
}