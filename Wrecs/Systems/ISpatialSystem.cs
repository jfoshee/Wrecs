using Wrecs.Core;

namespace Wrecs.Systems;

public interface ISpatialSystem : ISystemWithEntities
{
    float GetDistance(IEntity e1, IEntity e2);
}
