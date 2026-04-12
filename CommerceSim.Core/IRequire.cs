namespace CommerceSim.Core;

public interface IRequire<T>
{
    void Inject(T dependency);
}
