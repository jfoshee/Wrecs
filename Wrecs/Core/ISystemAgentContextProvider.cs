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

/// <summary>
/// A system that provides its own <typeparamref name="TSnapshot"/> state into an
/// agent's context, but only for agents that declare <see cref="IAgentRequireSnapshot{T}"/>
/// for that snapshot type.
/// </summary>
public interface ISystemAgentContextProvider<TSnapshot> : ISystemAgentContextProvider where TSnapshot : struct, IStateSnapshot
{
    TSnapshot? BuildSnapshot(IAgent agent);

    void ISystemAgentContextProvider.PopulateAgentContext(IAgent agent, AgentContext context)
    {
        if (agent is not IAgentRequireSnapshot<TSnapshot>)
            return;
        var snapshot = BuildSnapshot(agent);
        if (snapshot.HasValue)
            context.AddSnapshot(snapshot.Value);
    }
}
