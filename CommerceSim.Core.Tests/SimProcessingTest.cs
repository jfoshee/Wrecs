namespace CommerceSim.Core.Tests;

public class SimProcessingTest
{
    [Fact(DisplayName = "Process Zero Offer")]
    public void ProcessZeroOffer()
    {
        var author = Mock.Of<Agent>();
        var authorState = new AgentState(moneyBalance: 2, resourceBalance: 3);
        var counterpartyState = new AgentState(moneyBalance: 4, resourceBalance: 9);
        var offer = new BuyOffer(Buyer: author, Price: 0, Resources: 0);
        var decision = new TakeOfferDecision(offer);

        Sim.ProcessOffer(decision, authorState, counterpartyState);

        authorState.MoneyBalance.Should().Be(2);
        authorState.ResourceBalance.Should().Be(3);
        counterpartyState.MoneyBalance.Should().Be(4);
        counterpartyState.ResourceBalance.Should().Be(9);
    }

    [Fact(DisplayName = "Process Non-Zero Offer")]
    public void ProcessNonZeroOffer()
    {
        var buyer = Mock.Of<Agent>();
        var buyerState = new AgentState(moneyBalance: 32, resourceBalance: 9);
        var sellerState = new AgentState(moneyBalance: 64, resourceBalance: 27);
        var offer = new BuyOffer(Buyer: buyer, Price: 7, Resources: 5);
        var decision = new TakeOfferDecision(offer);

        Sim.ProcessOffer(decision, buyerState, sellerState);

        buyerState.MoneyBalance.Should().Be(32 - 7);
        buyerState.ResourceBalance.Should().Be(9 + 5);
        sellerState.MoneyBalance.Should().Be(64 + 7);
        sellerState.ResourceBalance.Should().Be(27 - 5);
    }
}
