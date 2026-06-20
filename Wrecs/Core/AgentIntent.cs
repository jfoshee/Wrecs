namespace Wrecs.Core;

public interface IAgentIntentAction { }

public record AgentIntent(IEnumerable<IAgentIntentAction> Actions)
{
    public static AgentIntent Empty { get; } = new AgentIntent([]);

    public AgentIntent(params IAgentIntentAction[] actions) : this((IEnumerable<IAgentIntentAction>)actions) { }
}
