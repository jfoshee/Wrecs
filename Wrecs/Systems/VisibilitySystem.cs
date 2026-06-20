using Wrecs.Core;

namespace Wrecs.Systems;

public record struct VisibilitySnapshot(IReadOnlyList<IEntity> VisibleEntities) : IStateSnapshot;

/// <summary>
/// Gives Agents the ability to see what Entities are nearby
/// </summary>
public class VisibilitySystem :
    ISystemAgentContextProvider<VisibilitySnapshot>,
    IRequire<Spatial1DSystem>
{
    private Spatial1DSystem? _spatialSystem;
    public void Inject(Spatial1DSystem dependency) => _spatialSystem = dependency;

    public VisibilitySnapshot? BuildSnapshot(IAgent agent)
    {
        if (_spatialSystem is null)
            throw new InvalidOperationException("Spatial1DSystem dependency not injected.");

        const float MaxDistance = 2;

        var entities = _spatialSystem.GetEntities();
        var visibleEntities = entities.Where(e => e != agent && _spatialSystem.GetDistance(agent, e) <= MaxDistance)
                                      .ToList();
        return new(visibleEntities);
    }
}
