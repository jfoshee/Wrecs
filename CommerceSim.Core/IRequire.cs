namespace CommerceSim.Core;

interface IRequire<T>
{
    void Inject(T dependency);
}
