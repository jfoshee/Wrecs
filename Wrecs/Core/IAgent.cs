namespace Wrecs.Core;

public interface IAgentContext
{
    bool HasSnapshot<T>() where T : IStateSnapshot;
    T GetSnapshot<T>() where T : IStateSnapshot;
}

public interface IAgent : IEntity
{
    IEnumerable<Type> GetRequiredSnapshots();
    Intent GetIntent(IAgentContext context);
}
