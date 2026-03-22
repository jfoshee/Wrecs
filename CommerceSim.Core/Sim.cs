namespace CommerceSim.Core;

public record class Offer(IAgent Author, int Price, int Resources)
{
    public bool Used { get; set; }
}

public record class BuyOffer(IAgent Buyer, int Price, int Resources) : Offer(Buyer, Price, Resources);
public record class SellOffer(IAgent Seller, int Price, int Resources) : Offer(Seller, Price, Resources);

public record struct AgentStateSnapshot(int MoneyBalance, int ResourceBalance)
{
    internal AgentStateSnapshot(AgentState state) : this(state.MoneyBalance, state.ResourceBalance) { }
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

public interface IPolicy
{
    bool CanExecute(Offer offer);

    /// <summary>
    /// Called after a trade has been executed.
    /// </summary>
    void OnExecuted(Offer offer);
}

public class OfferSingleUsePolicy : IPolicy
{
    public bool CanExecute(Offer offer) => !offer.Used;

    public void OnExecuted(Offer offer)
    {
        offer.Used = true;
    }
}

public interface IAgent
{
    /// <summary>
    /// Returns a unique name for this agent.
    /// </summary>
    public string Name { get; }


    /// <summary>
    /// Decide what to do with this tick, given the current state and available offers.
    /// </summary>
    /// <param name="state">The current state of the agent.</param>
    /// <param name="offers">The list of available offers.</param>
    /// <returns>The decision made by the agent.</returns>
    public Decision Decide(AgentStateSnapshot state, List<Offer> offers);
}

internal class AgentState(int moneyBalance = 0, int resourceBalance = 0)
{
    public int MoneyBalance { get; set; } = moneyBalance;
    public int ResourceBalance { get; set; } = resourceBalance;

    public AgentState(AgentStateSnapshot snapshot) :
        this(snapshot.MoneyBalance, snapshot.ResourceBalance)
    { }
}

public class Sim : ISimulator
{
    private readonly List<IAgent> _agents = [];
    private readonly Dictionary<IAgent, AgentState> _agentStates = [];
    private readonly List<Offer> _availableOffers = [];
    private readonly List<IPolicy> _policies = [new OfferSingleUsePolicy()];

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
        // ? should we randomize order of agents?
        // Decision making phase
        var decisions = new Dictionary<IAgent, Decision>();
        foreach (var agent in _agents)
        {
            var state = _agentStates[agent];
            var decision = agent.Decide(new(state), _availableOffers);
            decisions[agent] = decision;
        }

        // Processing phase
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

    void ProcessOffer(TakeOfferDecision decision,
                      AgentState authorState,
                      AgentState counterpartyState)
    {
        var offer = decision.Offer;
        foreach (var policy in _policies)
        {
            if (!policy.CanExecute(offer))
                return;
        }
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
        // Update policy state
        foreach (var policy in _policies)
        {
            policy.OnExecuted(offer);
        }
    }
}
