using Wrecs.Core;

namespace Wrecs.Systems;

public record struct VisibilitySnapshot(IReadOnlyList<IEntity> VisibleEntities) : IStateSnapshot;

/// <summary>
/// Gives Agents the ability to see what Entities are nearby
/// </summary>
public class VisibilitySystem(float maxDistance = 1) :
    ISystemAgentContextProvider<VisibilitySnapshot>,
    IRequire<ISpatialSystem>
{
    private ISpatialSystem? _spatialSystem;
    public void Inject(ISpatialSystem dependency) => _spatialSystem = dependency;

    public VisibilitySnapshot? BuildSnapshot(IAgent agent)
    {
        if (_spatialSystem is null)
            throw new InvalidOperationException($"{nameof(ISpatialSystem)} dependency not injected.");

        var entities = _spatialSystem.GetEntities();
        var visibleEntities = entities.Where(e => e != agent && _spatialSystem.GetDistance(agent, e) <= maxDistance)
                                      .ToList();
        return new(visibleEntities);
    }
}
