namespace Wrecs.Tests.Sandboxes;

using QAction = Sandbox1D1Agent.ExplorerAction;
using QActionRow = Dictionary<Sandbox1D1Agent.ExplorerAction, float>;
using QState = Sandbox1D1Agent.ExplorerObservation;

class QLearning
{
    /// <summary>
    /// The Learning Rate (alpha) determines how much new information overrides old information.
    /// A value of 0 means the agent does not learn anything,
    /// while a value of 1 means the agent only considers the most recent information.
    /// </summary>
    public float LearningRate { get; set; } = 0.5f;

    /// <summary>
    /// The Discount Factor (gamma) determines the importance of future rewards.
    /// It is the coefficient applied to the max Q value of the _next_ state.
    /// A value of 0 means the agent only considers immediate rewards,
    /// while a value of 1 means the agent values future rewards equally to immediate rewards.
    /// </summary>
    public float DiscountFactor { get; set; } = 0.5f;

    /// <summary>
    /// The Reward Function returns a reward value based on the change in state after taking an action.
    /// For improvements in state the reward should be high.
    /// For regressions in state the reward should be low.
    /// </summary>
    public Func<QState, QAction, QState, float> RewardFunction { get; set; } = (_, _, _) => 0;


    /// <summary>
    /// The Q-table is a matrix where rows represent states (S) and columns represent actions (A).
    /// Each cell is updated via the Bellman Equation after every step the agent takes.
    /// </summary>
    private readonly Dictionary<QState, QActionRow> _q = [];

    public float Q(QState state, QAction action)
    {
        if (_q.TryGetValue(state, out QActionRow? row))
        {
            if (row.TryGetValue(action, out float value))
            {
                return value;
            }
        }
        return default;
    }

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

    /// <summary>
    /// Incrementally "learn" from experience by updating the Q value
    /// for the given state and action
    /// based on the reward received and the max Q value of the new state.
    /// </summary>
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

        // Max Q of each state should be zero
        foreach (var state in AllStates())
        {
            subject.MaxQ(state).Should().Be(0);
            // subject.ChooseAction(state).Should().Be(default);
        }

        // Each Q value should start as zero
        foreach (var state in AllStates())
            foreach (var action in AllActions)
                subject.Q(state, action).Should().Be(0);
    }

    [Fact(DisplayName = "Set Q Value for given State and Action")]
    public void SetQValue()
    {
        var subject = new QLearning();
        var state = new QState(42, 24, true, false, true, false);
        var action = QAction.MoveLeft;
        var expected = 0.5f;

        subject.SetQ(state, action, expected);
        subject.Q(state, action).Should().Be(expected);
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

    [Fact(DisplayName = "Update Q: No Learning")]
    public void UpdatingQNoLearning()
    {
        // Without Learning the Q remains unchanged
        var subject = new QLearning
        {
            LearningRate = 0,
            DiscountFactor = 99,
            RewardFunction = (_, _, _) => 999
        };
        var state = new QState(10, 100, true, false, true, false);
        var action = QAction.MoveLeft;
        var newState = new QState(20, 300, true, false, true, false);
        var expected = 16;
        subject.SetQ(state, action, expected);

        subject.UpdateQ(state, action, newState);

        // With no learning, no matter the reward, the Q value remains unchanged
        subject.Q(state, action).Should().Be(expected);
    }

    [Fact(DisplayName = "Update Q: No Long-Term Learning: Discount Factor = 0")]
    public void UpdatingQNoLongTermLearning()
    {
        var subject = new QLearning
        {
            LearningRate = 1,
            DiscountFactor = 0,
            RewardFunction = (_, _, _) => 32f
        };
        var state = new QState(10, 100, true, false, true, false);
        var action = QAction.Sell;
        var newState = new QState(20, 300, true, false, true, false);
        subject.SetQ(state, action, 86);

        subject.UpdateQ(state, action, newState);

        // With a discount factor (gamma) of 0 the new Q value becomes the current reward,
        // the existing Q value is cancelled out
        subject.Q(state, action).Should().Be(32);
    }

    [Fact(DisplayName = "Update Q: Includes discounted MaxQ for next state")]
    public void UpdatingQIncludesDiscountedMaxQ()
    {
        var subject = new QLearning
        {
            LearningRate = 1,
            DiscountFactor = 0.5f,
            RewardFunction = (_, _, _) => 32f
        };
        var state = new QState(10, 100, true, false, true, false);
        var action = QAction.Sell;
        var newState = new QState(20, 300, true, false, true, false);
        subject.SetQ(state, action, 86);
        // Setup the "row" q values for the new state
        // Which is used to "predict" the future reward
        subject.SetQ(newState, QAction.MoveLeft, 10);
        subject.SetQ(newState, QAction.MoveRight, 70); // <- Will be max Q for new state
        subject.SetQ(newState, QAction.Sell, 15);

        subject.UpdateQ(state, action, newState);

        // With a discount factor (gamma) of 0.5 the new Q value becomes the current reward + discounted max Q for next state
        // The existing Q value is cancelled out
        subject.Q(state, action).Should().Be(32 + 0.5f * 70);
    }

    [Fact(DisplayName = "Update Q: Putting it all together")]
    public void UpdatingQPuttingItAllTogether()
    {
        var subject = new QLearning
        {
            LearningRate = 0.25f,
            DiscountFactor = 0.5f,
            RewardFunction = (_, _, _) => 32
        };
        var state = new QState(10, 100, true, false, true, false);
        var action = QAction.Sell;
        var newState = new QState(20, 300, true, false, true, false);
        subject.SetQ(state, action, 1024);
        // Setup the "row" q values for the new state
        // Which is used to "predict" the future reward
        subject.SetQ(newState, QAction.MoveLeft, 10);
        subject.SetQ(newState, QAction.MoveRight, 70); // <- Will be max Q for new state
        subject.SetQ(newState, QAction.Sell, 15);

        subject.UpdateQ(state, action, newState);

        subject.Q(state, action).Should().Be(1024 + 0.25f * (32 + 0.5f * 70 - 1024));
    }
}
