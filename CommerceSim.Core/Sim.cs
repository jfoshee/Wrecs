namespace CommerceSim.Core;

public record struct Trade(Offer Offer,
                           AgentStateSnapshot SellerState,
                           AgentStateSnapshot BuyerState,
                           int Price,
                           int Resources);

public class Sim : ISimulator
{
    private readonly List<IAgent> _agents = [];
    private readonly List<ISource> _sources = [];
    private readonly Dictionary<IAgent, AgentState> _agentStates = [];
    private readonly List<Offer> _availableOffers = [];

    private readonly List<ITradePolicy> _tradePolicies = [
        new OfferSingleUsePolicy(),
        new CannotCreateResourcesPolicy(),
        new CannotCreateMoneyPolicy()
    ];
    private readonly List<IGrantPolicy> _grantPolicies = [
        new NoNegativeGrantsPolicy()
    ];

    public AgentStateSnapshot GetState(IAgent agent) => new(_agentStates[agent]);

    public IReadOnlyDictionary<int, AgentStateSnapshot> GetStateSnapshot() =>
        _agentStates.ToDictionary(kvp => kvp.Key.Id, kvp => new AgentStateSnapshot(kvp.Value));

    public IReadOnlyDictionary<int, string> GetAgentNames() =>
        _agents.ToDictionary(a => a.Id, a => a.Name);

    public void InitAgents(params (IAgent agent, AgentStateSnapshot state)[] initialAgents)
    {
        _agents.Clear();
        _agentStates.Clear();
        foreach (var (agent, state) in initialAgents)
        {
            _agents.Add(agent);
            _agentStates[agent] = new(state);
        }
    }

    public void InitSources(params ISource[] sources)
    {
        _sources.Clear();
        _sources.AddRange(sources);
    }

    public void InitOffers(params Offer[] initialOffers)
    {
        _availableOffers.Clear();
        _availableOffers.AddRange(initialOffers);
    }

    // Advance simulation by one tick
    public void Tick()
    {
        // Hack context
        var context = new Context(this, new(), _agentStates.Keys);

        // Grant phase
        // (Run first so that on first tick grants can be used for seeding agents)
        var grants = _sources.SelectMany(s => s.CreateGrants(context));
        foreach (var grant in grants)
        {
            // Skip grants that violate policies
            if (_grantPolicies.Any(p => !p.CanExecute(grant)))
                continue;
            var state = _agentStates[grant.Recipient];
            state.MoneyBalance += grant.Money;
            state.ResourceBalance += grant.Resources;
        }

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

    private void ProcessOffer(TakeOfferDecision decision,
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
        foreach (var policy in _tradePolicies)
        {
            if (!policy.CanExecute(trade))
                return;
        }
        // Execute the trade
        Execute(offer, authorState, counterpartyState);
        // Update policy state
        foreach (var policy in _tradePolicies)
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

    internal class AgentState(int moneyBalance = 0, int resourceBalance = 0)
    {
        public int MoneyBalance { get; set; } = moneyBalance;
        public int ResourceBalance { get; set; } = resourceBalance;

        public AgentState(AgentStateSnapshot snapshot) :
            this(snapshot.MoneyBalance, snapshot.ResourceBalance)
        { }
    }
}
