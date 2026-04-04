using CommerceSim.Core.Agents;
using Microsoft.Extensions.AI;
using OllamaSharp;
using Xunit.Abstractions;

namespace CommerceSim.Core.Tests;

public class XUnitOutput(ITestOutputHelper output) : IOutput
{
    public void WriteLine(string message) => output.WriteLine(message);
}

public class LlmAgentTests(ITestOutputHelper output)
{
    private readonly IOutput _output = new XUnitOutput(output);

    private static IChatClient CreateChatClient() =>
        // new OllamaApiClient("http://localhost:11434", "qwen3.5:9b");
        new OllamaApiClient("http://localhost:11434", "llama3.2:3b");

    [Fact(DisplayName = "LLM agent takes obvious sell offer", Skip = "slow")]
    public void LlmAgentTakesObviousSellOffer()
    {
        var chatClient = CreateChatClient();
        var llmAgent = new LlmAgent(chatClient, _output);
        var seller = Mock.Of<ICommerceAgent>(a => a.Name == "Seller" && a.Id == AgentId.Next());
        var sim = new CommerceSystem();
        sim.InitAgents((llmAgent, new(MoneyBalance: 100, ResourceBalance: 0)),
                       (seller, new(MoneyBalance: 0, ResourceBalance: 50)));
        // Very cheap offer - LLM should take it
        sim.InitOffers(new SellOffer(seller, Price: 1, Resources: 10));

        sim.Tick();

        // Verify LLM took the offer
        var llmState = sim.GetState(llmAgent);
        llmState.MoneyBalance.Should().Be(99);
        llmState.ResourceBalance.Should().Be(10);
        var sellerState = sim.GetState(seller);
        sellerState.MoneyBalance.Should().Be(1);
        sellerState.ResourceBalance.Should().Be(40);
    }

    [Fact(DisplayName = "LLM agent takes obvious buy offer", Skip = "slow")]
    public void LlmAgentTakesObviousBuyOffer()
    {
        var chatClient = CreateChatClient();
        var llmAgent = new LlmAgent(chatClient, _output);
        var buyer = Mock.Of<ICommerceAgent>(a => a.Name == "Buyer" && a.Id == AgentId.Next());
        var sim = new CommerceSystem();
        sim.InitAgents((llmAgent, new(MoneyBalance: 0, ResourceBalance: 100)),
                       (buyer, new(MoneyBalance: 500, ResourceBalance: 0)));
        // Very generous buy offer - LLM should take it
        sim.InitOffers(new BuyOffer(buyer, Price: 100, Resources: 1));

        sim.Tick();

        // Verify LLM took the offer
        var llmState = sim.GetState(llmAgent);
        llmState.MoneyBalance.Should().Be(100);
        llmState.ResourceBalance.Should().Be(99);
        var buyerState = sim.GetState(buyer);
        buyerState.MoneyBalance.Should().Be(400);
        buyerState.ResourceBalance.Should().Be(1);
    }

    [Fact(DisplayName = "LLM agent makes sell offer that gets taken", Skip = "slow")]
    public void LlmAgentMakesSellOfferThatGetsTaken()
    {
        var chatClient = CreateChatClient();
        var llmAgent = new LlmAgent(chatClient, _output);
        var buyer = new AlwaysBuyingTaker();
        var sim = new CommerceSystem();
        var initialLlm = new AgentStateSnapshot(MoneyBalance: 0, ResourceBalance: 100);
        var initialBuyer = new AgentStateSnapshot(MoneyBalance: 1000, ResourceBalance: 0);
        sim.InitAgents((llmAgent, initialLlm), (buyer, initialBuyer));

        sim.Tick(); // LLM makes offer
        sim.Tick(); // Buyer takes it

        // Verify state changed - LLM sold something
        var llmState = sim.GetState(llmAgent);
        var buyerState = sim.GetState(buyer);

        // LLM should have gained money and lost resources
        llmState.MoneyBalance.Should().BeGreaterThan(0);
        llmState.ResourceBalance.Should().BeLessThan(100);
        // Buyer should have lost money and gained resources
        buyerState.MoneyBalance.Should().BeLessThan(1000);
        buyerState.ResourceBalance.Should().BeGreaterThan(0);
    }

    [Fact(DisplayName = "LLM agent makes buy offer that gets taken", Skip = "slow")]
    public void LlmAgentMakesBuyOfferThatGetsTaken()
    {
        var chatClient = CreateChatClient();
        var llmAgent = new LlmAgent(chatClient, _output);
        var seller = new AlwaysSellingTaker();
        var sim = new CommerceSystem();
        var initialLlm = new AgentStateSnapshot(MoneyBalance: 1000, ResourceBalance: 0);
        var initialSeller = new AgentStateSnapshot(MoneyBalance: 0, ResourceBalance: 100);
        sim.InitAgents((llmAgent, initialLlm), (seller, initialSeller));

        sim.Tick(); // LLM makes offer
        sim.Tick(); // Seller takes it

        // Verify state changed - LLM bought something
        var llmState = sim.GetState(llmAgent);
        var sellerState = sim.GetState(seller);

        // LLM should have lost money and gained resources
        llmState.MoneyBalance.Should().BeLessThan(1000);
        llmState.ResourceBalance.Should().BeGreaterThan(0);
        // Seller should have gained money and lost resources
        sellerState.MoneyBalance.Should().BeGreaterThan(0);
        sellerState.ResourceBalance.Should().BeLessThan(100);
    }

    [Fact(DisplayName = "Two LLM agents trade with each other", Skip = "slow")]
    public void TwoLlmAgentsTrade()
    {
        var chatClient = CreateChatClient();
        var agent1 = new LlmAgent(chatClient, _output);
        var agent2 = new LlmAgent(chatClient, _output);
        var sim = new CommerceSystem();
        var initial1 = new AgentStateSnapshot(MoneyBalance: 100, ResourceBalance: 50);
        var initial2 = new AgentStateSnapshot(MoneyBalance: 50, ResourceBalance: 100);
        sim.InitAgents((agent1, initial1), (agent2, initial2));

        // Run several ticks
        for (int i = 0; i < 10; i++)
            sim.Tick();

        // At least one trade should have happened
        var state1 = sim.GetState(agent1);
        var state2 = sim.GetState(agent2);

        // Combined totals should remain the same (conservation)
        (state1.MoneyBalance + state2.MoneyBalance).Should().Be(150);
        (state1.ResourceBalance + state2.ResourceBalance).Should().Be(150);

        // At least one agent's state should have changed
        var stateChanged = state1 != initial1 || state2 != initial2;
        stateChanged.Should().BeTrue("LLM agents should have traded");
    }
}
