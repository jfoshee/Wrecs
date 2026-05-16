using Wrecs.Core;

namespace Wrecs.Systems.Commercial;

public class ProximityResourceSource(int resourcesGranted, int intervalTicks, double proximity) : IResourceSource, ISpatialEntity, IRequire<SpatialSystem>
{
    public int Id { get; } = EntityId.Next();

    public string Name => nameof(ProximityResourceSource);

    private ICommercialAgent? _nearbyAgent = null;
    private int _nearbyTimeTicks = 0;

    private SpatialSystem _spatial = null!;
    public void Inject(SpatialSystem dependency)
    {
        _spatial = dependency;
    }

    public IEnumerable<ResourceFlow> CreateFlows(FlowContext context)
    {
        var spatial = _spatial;
        var agents = context.Entities.OfType<ICommercialAgent>();
        // If we aren't already tracking a nearby agent, look for one within proximity
        if (_nearbyAgent is null)
        {
            foreach (var agent in agents)
            {
                var agentDistance = spatial.GetDistance(this, agent);
                if (agentDistance <= proximity)
                {
                    _nearbyAgent = (ICommercialAgent?)agent;
                    break;
                }
            }
        }
        // If nobody is nearby, do nothing
        if (_nearbyAgent is null)
            yield break;
        // Increment how long the nearby agent has been nearby
        _nearbyTimeTicks++;
        // If they've been nearby long enough, grant them resources and reset
        if (_nearbyTimeTicks >= intervalTicks)
        {
            yield return ResourceFlow.Credit(recipient: _nearbyAgent, resources: resourcesGranted);
            // Reset tracking
            _nearbyTimeTicks = 0;
            _nearbyAgent = null;
        }
    }
}
