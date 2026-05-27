namespace Wrecs.Core;

/// <summary>
/// A system that contributes snapshots to an agent's context during the
/// centralized agent-invocation phase. The Sim calls PopulateContext on all
/// implementing systems before invoking IAgent.GetIntent.
/// </summary>
public interface IBuildAgentContext : ISystem
{
    void PopulateContext(IAgent agent, AgentContext context);
}
