using Wrecs.Core;

namespace Wrecs.Systems;

public interface ISpatialSystem
{
    float GetDistance(IEntity e1, IEntity e2);
}
