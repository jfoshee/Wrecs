namespace Wrecs.Core;

public interface IRequire;

public interface IRequire<T> : IRequire where T : ISystem
{
    void Inject(T dependency);
}
