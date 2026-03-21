namespace CommerceSim.Core.Tests;

public class SimProcessingTest
{
    [Fact(DisplayName = "Process Zero Buy Offer")]
    public void ProcessZeroBuyOffer()
    {
        var buyer = Mock.Of<IAgent>();
        var buyerState = new AgentState(moneyBalance: 2, resourceBalance: 3);
        var sellerState = new AgentState(moneyBalance: 4, resourceBalance: 9);
        var offer = new BuyOffer(Buyer: buyer, Price: 0, Resources: 0);
        var decision = new TakeOfferDecision(offer);

        Sim.ProcessOffer(decision, buyerState, sellerState);

        buyerState.MoneyBalance.Should().Be(2);
        buyerState.ResourceBalance.Should().Be(3);
        sellerState.MoneyBalance.Should().Be(4);
        sellerState.ResourceBalance.Should().Be(9);
    }

    [Fact(DisplayName = "Process Non-Zero Buy Offer")]
    public void ProcessNonZeroBuyOffer()
    {
        var buyer = Mock.Of<IAgent>();
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

    [Fact(DisplayName = "Process Non-Zero Sell Offer")]
    public void ProcessNonZeroSellOffer()
    {
        var seller = Mock.Of<IAgent>();
        var sellerState = new AgentState(moneyBalance: 32, resourceBalance: 9);
        var buyerState = new AgentState(moneyBalance: 64, resourceBalance: 27);
        var offer = new SellOffer(Seller: seller, Price: 7, Resources: 5);
        var decision = new TakeOfferDecision(offer);

        Sim.ProcessOffer(decision, sellerState, buyerState);

        sellerState.MoneyBalance.Should().Be(32 + 7);
        sellerState.ResourceBalance.Should().Be(9 - 5);
        buyerState.MoneyBalance.Should().Be(64 - 7);
        buyerState.ResourceBalance.Should().Be(27 + 5);
    }
}
