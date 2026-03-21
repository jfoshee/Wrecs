namespace CommerceSim.Core;

public record class Offer(Agent Author, int Price, int Resources);
public record class BuyOffer(Agent Buyer, int Price, int Resources) : Offer(Buyer, Price, Resources);
public record class SellOffer(Agent Seller, int Price, int Resources) : Offer(Seller, Price, Resources);

public class AgentState(int moneyBalance = 0, int resourceBalance = 0)
{
    public int MoneyBalance { get; set; } = moneyBalance;
    public int ResourceBalance { get; set; } = resourceBalance;
}

public abstract class Decision
{
}

public class DoNothingDecision : Decision
{
}

public class TakeOfferDecision(Offer offer) : Decision
{
    public Offer Offer { get; } = offer;
}

public class MakeOfferDecision(Offer offer) : Decision
{
    public Offer Offer { get; } = offer;
}

public abstract class Agent
{
    // Decide what to do with this tick, given the current state and opportunities
    public abstract Decision Decide(AgentState state, List<Offer> opportunities);
}

public class Sim
{
    private readonly List<Agent> _agents = [];
    private readonly Dictionary<Agent, AgentState> _agentStates = [];
    private readonly List<Offer> _availableOffers = [];

    public AgentState GetState(Agent agent) => _agentStates[agent];

    public void InitAgents(params (Agent, AgentState?)[] initialAgents)
    {
        _agents.Clear();
        _agentStates.Clear();
        foreach (var (agent, state) in initialAgents)
        {
            _agents.Add(agent);
            _agentStates[agent] = state ?? new AgentState();
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
        // ? should we randomize order of agents?
        // Decision making phase
        var decisions = new Dictionary<Agent, Decision>();
        foreach (var agent in _agents)
        {
            var state = _agentStates[agent];
            var decision = agent.Decide(state, _availableOffers);
            decisions[agent] = decision;
        }

        // Processing phase
        foreach (var (agent, decision) in decisions)
        {
            switch (decision)
            {
                case DoNothingDecision:
                    break;
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

    public static void ProcessOffer(TakeOfferDecision decision, AgentState authorState, AgentState counterpartyState)
    {
        var offer = decision.Offer;
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
        // transfer money from buyer to seller
        buyer.MoneyBalance -= offer.Price;
        seller.MoneyBalance += offer.Price;
        // transfer resources from seller to buyer
        buyer.ResourceBalance += offer.Resources;
        seller.ResourceBalance -= offer.Resources;
    }
}
