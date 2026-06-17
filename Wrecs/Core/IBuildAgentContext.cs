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

/// <summary>
/// A system that provides its own <typeparamref name="TSnapshot"/> state into an
/// agent's context, but only for agents that declare <see cref="IRequireSnapshot{T}"/>
/// for that snapshot type.
/// </summary>
public interface IBuildAgentContext<TSnapshot> : IBuildAgentContext where TSnapshot : struct, IStateSnapshot
{
    TSnapshot? BuildSnapshot(IAgent agent);

    void IBuildAgentContext.PopulateContext(IAgent agent, AgentContext context)
    {
        if (agent is not IRequireSnapshot<TSnapshot>)
            return;
        var snapshot = BuildSnapshot(agent);
        if (snapshot.HasValue)
            context.AddSnapshot(snapshot.Value);
    }
}
