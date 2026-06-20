namespace Wrecs.Core;

/// <summary>
/// A system that contributes snapshots to an agent's context during the
/// centralized agent-invocation phase. The Sim calls PopulateAgentContext on all
/// implementing systems before invoking IAgent.GetIntent.
/// </summary>
public interface ISystemAgentContextBuilder : ISystem
{
    void PopulateAgentContext(IAgent agent, AgentContext context);
}
