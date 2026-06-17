namespace Wrecs.Core;

public interface IAgentContext
{
    bool HasSnapshot<T>() where T : IStateSnapshot;
    T GetSnapshot<T>() where T : IStateSnapshot;
}

public interface IAgent : IEntity
{
    Intent GetIntent(IAgentContext context);
}

/// <summary>
/// Declares that an agent needs a snapshot of type <typeparamref name="T"/>
/// in its <see cref="IAgentContext"/>. Implemented by agents; checked by
/// <see cref="IBuildAgentContext{TSnapshot}"/> implementations.
/// </summary>
public interface IRequireSnapshot<T> where T : struct, IStateSnapshot;
