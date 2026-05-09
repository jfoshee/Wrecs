using System.Linq.Expressions;

namespace CommerceSim.Core;

public class SystemLogger<TSystem>(IOutput output) : ISystem, IRequire<TSystem>, ITickPhases
    where TSystem : class, ISystem
{
    private static readonly Func<TSystem, IReadOnlyList<IEntity>> _getEntities = CreateGetEntities();
    private static readonly Func<TSystem, IEntity, IStateSnapshot> _getState = CreateGetState();

    private TSystem? _system;
    private int _tickCount;

    public void Inject(TSystem dependency)
    {
        _system = dependency;
    }

    public void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState) { }

    public void PrepareUpdates()
    {
        var system = _system ?? throw new InvalidOperationException($"{nameof(SystemLogger<>)} requires an injected {typeof(TSystem).Name}.");
        output.WriteLine($"--- {typeof(TSystem).Name} Tick {_tickCount} ---");

        foreach (var entity in _getEntities(system))
        {
            output.WriteLine($"{entity.Name} ({entity.Id}): {_getState(system, entity)}");
        }
    }

    public void ApplyUpdates()
    {
        _tickCount++;
    }

    public void Tick()
    {
        PrepareUpdates();
        ApplyUpdates();
    }

    private static Type GetStateSystemInterface() =>
        typeof(TSystem).GetInterfaces()
            .SingleOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISystem<,>))
        ?? throw new InvalidOperationException($"{typeof(TSystem).Name} must implement {typeof(ISystem<,>).Name} to be used with {nameof(SystemLogger<TSystem>)}.");

    private static Func<TSystem, IReadOnlyList<IEntity>> CreateGetEntities()
    {
        var systemInterface = GetStateSystemInterface();
        var systemParameter = Expression.Parameter(typeof(TSystem), "system");
        var getEntitiesCall = Expression.Call(
            Expression.Convert(systemParameter, systemInterface),
            systemInterface.GetMethod(nameof(ISystem<IEntity, int>.GetEntities))!);

        return Expression.Lambda<Func<TSystem, IReadOnlyList<IEntity>>>(getEntitiesCall, systemParameter).Compile();
    }

    private static Func<TSystem, IEntity, IStateSnapshot> CreateGetState()
    {
        var systemInterface = GetStateSystemInterface();
        var systemParameter = Expression.Parameter(typeof(TSystem), "system");
        var entityParameter = Expression.Parameter(typeof(IEntity), "entity");
        var getStateCall = Expression.Call(
            Expression.Convert(systemParameter, systemInterface),
            systemInterface.GetMethod(nameof(ISystem<IEntity, int>.GetState))!,
            entityParameter);

        return Expression.Lambda<Func<TSystem, IEntity, IStateSnapshot>>(
            Expression.Convert(getStateCall, typeof(IStateSnapshot)),
            systemParameter,
            entityParameter).Compile();
    }
}
