using CommerceSim.Core.Spatial;

namespace CommerceSim.Core;

public class ProximityResourceSource(int resourcesGranted, int intervalTicks, double proximity) : ISource, ISpatialEntity, IRequire<SpatialSystem>
{
    private readonly int _id = EntityId.Next();
    public int Id => _id;

    public string Name => nameof(ProximityResourceSource);

    private ICommerceAgent? _nearbyAgent = null;
    private int _nearbyTimeTicks = 0;

    private SpatialSystem _spatial = null!;
    public void Inject(SpatialSystem dependency)
    {
        _spatial = dependency;
    }

    public IEnumerable<Grant> CreateGrants(Context context)
    {
        var spatial = _spatial;
        var agents = context.Entities.OfType<ICommerceAgent>();
        // If we aren't already tracking a nearby agent, look for one within proximity
        if (_nearbyAgent is null)
        {
            foreach (var agent in agents)
            {
                var agentDistance = spatial.GetDistance(this, agent);
                if (agentDistance <= proximity)
                {
                    _nearbyAgent = (ICommerceAgent?)agent;
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
            // Reset tracking
            _nearbyTimeTicks = 0;
            _nearbyAgent = null;
        }
    }
}
