using System.Text.Json;
using Microsoft.Extensions.AI;

namespace CommerceSim.Core.Agents;

public sealed class LlmAgent(IChatClient chatClient) : IAgent
{
    private readonly int _id = AgentId.Next();

    public string Name => $"LLM-{_id}";

    public Decision Decide(AgentStateSnapshot state, List<Offer> offers)
    {
        var availableOffers = offers
            .Where(o => o.Author != this && !o.Used)
            .Select((o, i) => new { Index = i, Type = o is BuyOffer ? "buy" : "sell", o.Price, o.Resources })
            .ToList();

        var prompt = $$"""
            You are a trading agent in a market simulation. Make a profitable decision.

            Your current state:
            - Money: {{state.MoneyBalance}}
            - Resources: {{state.ResourceBalance}}

            Available offers to take:
            {{(availableOffers.Count == 0 ? "None" : JsonSerializer.Serialize(availableOffers))}}

            You can:
            1. "nothing" - do nothing
            2. "take" - accept an existing offer (specify offerIndex)
            3. "buy" - post a buy offer (specify price and resources you want)
            4. "sell" - post a sell offer (specify price and resources to sell)

            Respond with ONLY valid JSON (no markdown):
            {"action": "nothing|take|buy|sell", "offerIndex": 0, "price": 10, "resources": 1}
            """;

        var response = chatClient.GetResponseAsync(prompt).GetAwaiter().GetResult();
        return ParseDecision(response.Text ?? "", offers.Where(o => o.Author != this && !o.Used).ToList());
    }

    private Decision ParseDecision(string responseText, List<Offer> availableOffers)
    {
        try
        {
            var json = responseText.Trim();
            if (json.StartsWith("```"))
            {
                var lines = json.Split('\n');
                json = string.Join('\n', lines.Skip(1).TakeWhile(l => !l.StartsWith("```")));
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var action = root.GetProperty("action").GetString()?.ToLowerInvariant();

            return action switch
            {
                "take" when root.TryGetProperty("offerIndex", out var idx) && idx.GetInt32() < availableOffers.Count
                    => new TakeOfferDecision(availableOffers[idx.GetInt32()]),
                "buy" => new MakeOfferDecision(new BuyOffer(this, root.GetProperty("price").GetInt32(), root.GetProperty("resources").GetInt32())),
                "sell" => new MakeOfferDecision(new SellOffer(this, root.GetProperty("price").GetInt32(), root.GetProperty("resources").GetInt32())),
                _ => new DoNothingDecision()
            };
        }
        catch
        {
            return new DoNothingDecision();
        }
    }
}
