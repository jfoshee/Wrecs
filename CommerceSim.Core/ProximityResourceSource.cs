using CommerceSim.Core.Spatial;

namespace CommerceSim.Core;

public class ProximityResourceSource(int resourcesGranted, int intervalTicks, double proximity) : ISource, IEntity, IRequire<SpatialSystem>
{
    private readonly int _id = Agents.AgentId.Next();
    public int Id => _id;

    public string Name => nameof(ProximityResourceSource);

    private IAgent? _nearbyAgent = null;
    private int _nearbyTimeTicks = 0;

    private SpatialSystem _spatial = null!;
    public void Inject(SpatialSystem dependency)
    {
        _spatial = dependency;
    }

    public IEnumerable<Grant> CreateGrants(Context context)
    {
        var spatial = _spatial;
        var myPosition = spatial.GetPosition(this);
        var agents = context.Entities.OfType<IAgent>();
        // If we aren't already tracking a nearby agent, look for one within proximity
        if (_nearbyAgent is null)
        {
            foreach (var agent in agents)
            {
                var p = spatial.GetPosition(agent);
                var agentDistance = spatial.GetDistance(myPosition, p);
                if (agentDistance <= proximity)
                {
                    _nearbyAgent = (IAgent?)agent;
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
            yield return new Grant(Recipient: _nearbyAgent, Money: 0, Resources: resourcesGranted);
            _nearbyTimeTicks = 0;
        }
    }
}
