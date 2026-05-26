namespace Wrecs.Core;

public interface IAgentContext
{
    bool HasSnapshot<T>() where T : IStateSnapshot;
    bool Has<T>();
    T GetSnapshot<T>() where T : IStateSnapshot;
    T Get<T>();
}

public interface IAgent : IEntity
{
    IEnumerable<Type> GetRequiredSnapshots();
    Intent GetIntent(IAgentContext context);
}
