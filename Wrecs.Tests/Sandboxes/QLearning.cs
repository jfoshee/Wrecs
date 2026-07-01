namespace Wrecs.Tests.Sandboxes;

using QAction = Sandbox1D1Agent.ExplorerAction;
using QActionRow = Dictionary<Sandbox1D1Agent.ExplorerAction, float>;
using QState = Sandbox1D1Agent.ExplorerObservation;

class QLearning
{
    // The Q-table is a matrix where rows represent states (S) and columns represent actions (A)
    private readonly Dictionary<QState, QActionRow> _q = [];

    public Func<QState, QAction, QState, float> RewardFunction { get; set; } = (_, _, _) => 0;

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

    private static QAction MaxQAction(QActionRow row) => Enumerable.MaxBy(row, cell => cell.Value).Key;

    internal float MaxQ(QState state)
    {
        if (_q.TryGetValue(state, out QActionRow? row))
        {
            return row.Values.Max();
        }
        return default;
    }


    public QAction ChooseAction(QState state)
    {
        if (_q.TryGetValue(state, out QActionRow? row))
        {
            return MaxQAction(row);
        }
        return default;
    }

    // alpha = learning rate
    public float LearningRate { get; set; } = 0.5f;

    // gamma = discount factor
    public float DiscountFactor { get; set; } = 0.5f;

    public void UpdateQ(QState priorState, QAction action, QState newState)
    {
        var reward = RewardFunction(priorState, action, newState);
        // The table is updated using the Bellman Equation after every step the agent takes:
        // \(Q(s,a)\leftarrow Q(s,a)+\alpha \left[r+\gamma \max _{a^{\prime }}Q(s^{\prime },a^{\prime })-Q(s,a)\right]\)
        var Q_sa = Q(priorState, action);
        Q_sa = Q_sa + LearningRate * (
            reward + DiscountFactor * MaxQ(newState) - Q_sa
        );
        SetQ(priorState, action, Q_sa);
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

    static float Reward(int moneyDelta, int resourceDelta, int ticksElapsed, int stepsTaken)
    {
        const int HardCodedResourceValue = 10;
        const float weightMoney = 100;
        const float weightAge = -1;
        const float weightSteps = -1;
        return weightMoney * (moneyDelta + resourceDelta * HardCodedResourceValue)
            + weightAge * ticksElapsed
            + weightSteps * stepsTaken;
    }

    static float Reward(QState priorState, QAction action, QState newState)
    {
        var moneyDelta = newState.MoneyBalance - priorState.MoneyBalance;
        var resourceDelta = newState.ResourceBalance - priorState.ResourceBalance;
        var ticksElapsed = 1;
        var stepsTaken = action == QAction.MoveLeft || action == QAction.MoveRight ? 1 : 0;
        return Reward(moneyDelta, resourceDelta, ticksElapsed, stepsTaken);
    }

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

        // Without a row set, should return defaults
        subject.MaxQ(state).Should().Be(0);
        subject.ChooseAction(state).Should().Be(default);

        // Only 1 Q value in row set to non-zero, that's the winner
        subject.SetQ(state, QAction.MoveLeft, 0.3f);
        subject.MaxQ(state).Should().Be(0.3f);
        subject.ChooseAction(state).Should().Be(QAction.MoveLeft);

        // Set other Q values in row
        subject.SetQ(state, QAction.Stay, 0.2f);
        subject.SetQ(state, QAction.MoveRight, 0.4f);
        subject.SetQ(state, QAction.Collect, 0.9f);
        subject.SetQ(state, QAction.Sell, 0.7f);

        // And the Action with the Max Q Value should be selected
        subject.MaxQ(state).Should().Be(0.9f);
        subject.ChooseAction(state).Should().Be(QAction.Collect);
    }
}
