namespace CommerceSim.Core;

public interface IRequire<T> : IEntity
{
    void Inject(T dependency);
}
