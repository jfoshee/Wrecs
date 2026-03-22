namespace CommerceSim.Core;

public record struct AgentStateSnapshot(int MoneyBalance, int ResourceBalance)
{
    internal AgentStateSnapshot(AgentState state) : this(state.MoneyBalance, state.ResourceBalance) { }
}

internal class AgentState(int moneyBalance = 0, int resourceBalance = 0)
{
    public int MoneyBalance { get; set; } = moneyBalance;
    public int ResourceBalance { get; set; } = resourceBalance;

    public AgentState(AgentStateSnapshot snapshot) :
        this(snapshot.MoneyBalance, snapshot.ResourceBalance)
    { }
}

public record struct Trade(Offer Offer,
                           AgentStateSnapshot SellerState,
                           AgentStateSnapshot BuyerState,
                           int Price,
                           int Resources);

public class Sim : ISimulator
{
    private readonly List<IAgent> _agents = [];
    private readonly Dictionary<IAgent, AgentState> _agentStates = [];
    private readonly List<Offer> _availableOffers = [];
    private readonly List<IPolicy> _policies = [
        new OfferSingleUsePolicy(),
        new CannotCreateResourcesPolicy(),
        new CannotCreateMoneyPolicy()
    ];

    public AgentStateSnapshot GetState(IAgent agent) => new(_agentStates[agent]);

    public IReadOnlyDictionary<string, AgentStateSnapshot> GetStateSnapshot() =>
        _agentStates.ToDictionary(kvp => kvp.Key.Name, kvp => new AgentStateSnapshot(kvp.Value));

    public void InitAgents(params (IAgent agent, AgentStateSnapshot state)[] initialAgents)
    {
        // Ensure Agent names are unique
        var duplicateNames = initialAgents.GroupBy(x => x.agent.Name)
                                          .Where(g => g.Count() > 1)
                                          .Select(g => $"'{g.Key}'");
        if (duplicateNames.Any())
            throw new ArgumentException($"Agent names must be unique. Duplicates: {string.Join(", ", duplicateNames)}");
        _agents.Clear();
        _agentStates.Clear();
        foreach (var (agent, state) in initialAgents)
        {
            _agents.Add(agent);
            _agentStates[agent] = new(state);
        }
    }

    public void InitOffers(params Offer[] initialOffers)
    {
        _availableOffers.Clear();
        _availableOffers.AddRange(initialOffers);
    }

    // Advance simulation by one tick
    public void Tick()
    {
        // Decision making phase
        var decisions = new List<(IAgent Agent, Decision Decision)>();
        foreach (var agent in _agents)
        {
            var state = _agentStates[agent];
            var decision = agent.Decide(new(state), _availableOffers);
            // decisions[agent] = decision;
            decisions.Add((agent, decision));
        }

        // Processing phase
        decisions = Shuffle(decisions);
        foreach (var (agent, decision) in decisions)
        {
            switch (decision)
            {
                case TakeOfferDecision takeOfferDecision:
                    var offer = takeOfferDecision.Offer;
                    _availableOffers.Remove(offer);
                    ProcessOffer(takeOfferDecision, _agentStates[offer.Author], _agentStates[agent]);
                    break;
                case MakeOfferDecision makeOfferDecision:
                    var newOffer = makeOfferDecision.Offer;
                    _availableOffers.Add(newOffer);
                    break;
            }
        }
    }

    private static readonly Random _random = new();

    /// <summary>
    /// Randomly shuffle the order of decisions to ensure fairness in processing and avoid bias based on agent order.
    /// </summary>
    private static List<(IAgent Agent, Decision Decision)> Shuffle(List<(IAgent Agent, Decision Decision)> decisions)
    {
        return [.. decisions.OrderBy(_ => _random.Next())];
    }

    void ProcessOffer(TakeOfferDecision decision,
                      AgentState authorState,
                      AgentState counterpartyState)
    {
        var offer = decision.Offer;
        // Construct a trade based on the offer
        var trade = new Trade(Offer: offer,
                              SellerState: offer is SellOffer ? new(authorState) : new(counterpartyState),
                              BuyerState: offer is BuyOffer ? new(authorState) : new(counterpartyState),
                              Price: offer.Price,
                              Resources: offer.Resources);
        // Check policies before executing the trade
        foreach (var policy in _policies)
        {
            if (!policy.CanExecute(trade))
                return;
        }
        // Execute the trade
        Execute(offer, authorState, counterpartyState);
        // Update policy state
        foreach (var policy in _policies)
        {
            policy.OnExecuted(trade);
        }
    }

    private static void Execute(Offer offer, AgentState authorState, AgentState counterpartyState)
    {
        AgentState buyer, seller;
        switch (offer)
        {
            case BuyOffer buyOffer:
                buyer = authorState;
                seller = counterpartyState;
                break;
            case SellOffer sellOffer:
                buyer = counterpartyState;
                seller = authorState;
                break;
            default:
                throw new InvalidOperationException("Unknown offer type");
        }
        // Transfer money from buyer to seller
        buyer.MoneyBalance -= offer.Price;
        seller.MoneyBalance += offer.Price;
        // Transfer resources from seller to buyer
        buyer.ResourceBalance += offer.Resources;
        seller.ResourceBalance -= offer.Resources;
    }
}
