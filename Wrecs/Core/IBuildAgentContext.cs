namespace Wrecs.Core;

/// <summary>
/// A system that provides its own <typeparamref name="TSnapshot"/> state into an
/// agent's context, but only for agents that declare <see cref="IRequireSnapshot{T}"/>
/// for that snapshot type.
/// </summary>
public interface IBuildAgentContext<TSnapshot> : ISystemAgentContextBuilder where TSnapshot : struct, IStateSnapshot
{
    TSnapshot? BuildSnapshot(IAgent agent);

    void ISystemAgentContextBuilder.PopulateAgentContext(IAgent agent, AgentContext context)
    {
        if (agent is not IRequireSnapshot<TSnapshot>)
            return;
        var snapshot = BuildSnapshot(agent);
        if (snapshot.HasValue)
            context.AddSnapshot(snapshot.Value);
    }
}
