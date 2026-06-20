namespace Wrecs.Core;

/// <summary>
/// A system that contributes snapshots to an agent's context during the
/// centralized agent-invocation phase.
/// The <see cref="Sim"/> calls <see cref="PopulateAgentContext"/> on all
/// implementing systems before invoking <see cref="IAgent.GetIntent"/>.
/// </summary>
public interface ISystemAgentContextProvider : ISystem
{
    void PopulateAgentContext(IAgent agent, AgentContext context);
}
