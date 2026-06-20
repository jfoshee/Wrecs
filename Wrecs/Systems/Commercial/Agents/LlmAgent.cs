using System.Text.Json;
using Microsoft.Extensions.AI;
using Wrecs.Core;

namespace Wrecs.Systems.Commercial.Agents;

public sealed class LlmAgent(IChatClient chatClient, IOutput? output = null) : ICommercialAgent
{
    public int Id { get; } = EntityId.Next();

    public string Name => $"LLM-{Id}";

    public AgentIntent GetIntent(IAgentContext context)
    {
        var state = context.GetCommercialSnapshot();
        var offers = context.GetSnapshot<OfferListSnapshot>().Offers?.ToList() ?? [];
        var availableOffers = offers
            .Where(o => o.Author != this && !o.Used)
            .Select((o, i) => new { Index = i, Type = o is BuyOffer ? "buy" : "sell", o.Price, o.Resources })
            .ToList();
        var prompt = $$"""
    You are a trading agent in a market simulation.

    Goal:
    - Make money over time by buying and selling resources.

    Offer semantics:
    - A "sell" offer means another agent is selling resources. If you take it, you BUY those resources.
    - A "buy" offer means another agent is buying resources. If you take it, you SELL those resources.

    Your state:
    - Money: {{state.MoneyBalance}}
    - Resources: {{state.ResourceBalance}}

    Available offers:
    {{(availableOffers.Count == 0 ? "[]" : JsonSerializer.Serialize(availableOffers))}}

    Available actions / schema for response:
    - {"action":"nothing"}
    - {"action":"take","offerIndex":<number>}
    - {"action":"buy","price":<number>,"resources":<number>}
    - {"action":"sell","price":<number>,"resources":<number>}

    Rules:
    - Only choose actions you can afford and complete immediately.
    - If there are no available offers, you may only create new offers or do nothing.
    - If an existing offer already gives you the deal you want, use "take" instead of creating a new offer.
    - Do not create a "buy" offer at a price and quantity that could already be satisfied by an existing "sell" offer.
    - Do not create a "sell" offer at a price and quantity that could already be satisfied by an existing "buy" offer.
    - Do not over-think this. No more than 3 rounds of thoughts.

    Response requirements:
    - Return valid JSON only.
    """;

        output?.WriteLine($"[{Name}] PROMPT:\n{prompt}");

        var response = chatClient.GetResponseAsync(prompt).GetAwaiter().GetResult();

        // Output all message contents including thinking
        foreach (var message in response.Messages)
        {
            foreach (var content in message.Contents)
            {
                var contentType = content.GetType().Name;
                var thinkingProp = content.GetType().GetProperty("Thinking");

                if (contentType.Contains("Thinking", StringComparison.OrdinalIgnoreCase) || thinkingProp is not null)
                {
                    var thinkingText = thinkingProp?.GetValue(content)?.ToString() ?? content.ToString();
                    output?.WriteLine($"[{Name}] THINKING:\n{thinkingText}");
                }
                else if (content is not TextContent)
                {
                    output?.WriteLine($"[{Name}] {contentType}:\n{content}");
                }
            }
        }

        output?.WriteLine($"[{Name}] RESPONSE:\n{response.Text}");

        return ParseDecision(response.Text ?? "", offers.Where(o => o.Author != this && !o.Used).ToList());
    }

    private AgentIntent ParseDecision(string responseText, List<Offer> availableOffers)
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
                    => new AgentIntent(new TakeOfferDecision(availableOffers[idx.GetInt32()])),
                "buy" => new AgentIntent(new MakeOfferDecision(new BuyOffer(this, root.GetProperty("price").GetInt32(), root.GetProperty("resources").GetInt32()))),
                "sell" => new AgentIntent(new MakeOfferDecision(new SellOffer(this, root.GetProperty("price").GetInt32(), root.GetProperty("resources").GetInt32()))),
                _ => throw new Exception("Failed to parse JSON: \n" + responseText)
            };
        }
        catch
        {
            return AgentIntent.Empty;
        }
    }
}
