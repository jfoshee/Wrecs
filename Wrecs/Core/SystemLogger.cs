using System.Linq.Expressions;

namespace Wrecs.Core;

public class SystemLogger<TSystem>(IOutput output) : ISystem, IRequire<TSystem>
    where TSystem : class, ISystem, IHasEntities, IHasEntityState
{
    private TSystem? _system;
    private int _tickCount;

    public void Inject(TSystem dependency)
    {
        _system = dependency;
    }

    public void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState) { }

    public void PrepareInternalUpdates()
    {
        var system = _system ?? throw new InvalidOperationException($"{nameof(SystemLogger<>)} requires an injected {typeof(TSystem).Name}.");
        output.WriteLine($"--- {typeof(TSystem).Name} Tick {_tickCount} ---");

        foreach (var entity in system.GetEntities())
        {
            output.WriteLine($"{entity.Name} ({entity.Id}): {system.GetState(entity)}");
        }
    }

    public void ApplyInternalUpdates()
    {
        _tickCount++;
    }
}
