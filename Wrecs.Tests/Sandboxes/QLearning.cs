namespace Wrecs.Tests.Sandboxes;

using QAction = Sandbox1D1Agent.ExplorerAction;
using QActionRow = Dictionary<Sandbox1D1Agent.ExplorerAction, float>;
using QState = Sandbox1D1Agent.ExplorerObservation;

class QLearning(int? seed = null)
{
    private readonly Random _random = seed.HasValue ? new Random(seed.Value) : Random.Shared;

    /// <summary>
    /// The Learning Rate (alpha) determines how much new information overrides old information.
    /// A value of 0 means the agent does not learn anything,
    /// while a value of 1 means the agent only considers the most recent information.
    /// </summary>
    public float LearningRate { get; set; } = 0.1f;

    /// <summary>
    /// The Discount Factor (gamma) determines the importance of future rewards.
    /// It is the coefficient applied to the max Q value of the _next_ state.
    /// A value of 0 means the agent only considers immediate rewards,
    /// while a value of 1 means the agent values future rewards equally to immediate rewards.
    /// </summary>
    public float DiscountFactor { get; set; } = 0.9f;

    /// <summary>
    /// The Exploration Probability (epsilon) determines how often the agent explores
    /// new actions versus exploiting known actions.
    /// A value of 0 means the agent always exploits known actions,
    /// while a value of 1 means the agent always explores new actions.
    /// New actions are randomly selected from the set of possible actions.
    /// </summary>
    public float ExplorationProbability { get; set; } = 0;

    /// <summary>
    /// The Reward Function returns a reward value based on the change in state after taking an action.
    /// For improvements in state the reward should be high.
    /// For regressions in state the reward should be low.
    /// </summary>
    public Func<QState, QAction, QState, float> RewardFunction { get; set; } = (_, _, _) => 0;

    public QAction[] AllActions { get; set; } = Enum.GetValues<QAction>();

    internal IEnumerable<float> GetAllQValues() => _q.SelectMany(row => row.Value.Values);

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
        return 0;
    }

    internal void SetQ(QState state, QAction action, float value)
    {
        if (!_q.TryGetValue(state, out QActionRow? row))
        {
            _q[state] = row = [];
        }

        row[action] = value;
    }

    private static QAction MaxQAction(QActionRow row) => Enumerable.MaxBy(row, cell => cell.Value).Key;

    internal float MaxQ(QState state)
    {
        if (_q.TryGetValue(state, out QActionRow? row))
        {
            return row.Values.Max();
        }
        return 0;
    }

    /// <summary>
    /// Return a randomly selected action from the set of all actions, excluding any specified actions.
    /// If all actions are excluded, a random action is selected from the set of all actions.
    /// </summary>
    internal QAction RandomAction(IEnumerable<QAction>? excluding)
    {
        var excluded = excluding?.ToHashSet() ?? [];
        var availableActions = AllActions.Except(excluded).ToArray();
        if (availableActions.Length == 0)
        {
            return AllActions[_random.Next(AllActions.Length)];
        }
        return availableActions[_random.Next(availableActions.Length)];
    }

    public QAction ChooseAction(QState state)
    {
        // TODO: Restrict to legal/allowed actions (set could be passed in)
        bool explore = _random.NextDouble() < ExplorationProbability;
        if (_q.TryGetValue(state, out QActionRow? row) && !explore)
        {
            return MaxQAction(row);
        }
        // Explore an action that has not yet been tried in this state, if possible.
        return RandomAction(excluding: row?.Keys);
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
        var Q_sa = Q(priorState, action);
        Q_sa += LearningRate * (
            reward + DiscountFactor * MaxQ(newState) - Q_sa
        );
        SetQ(priorState, action, Q_sa);
    }
}
