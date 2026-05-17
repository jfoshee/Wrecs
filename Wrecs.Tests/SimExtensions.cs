using Wrecs.Systems;

namespace Wrecs.Tests;

static class SimExtensions
{
    extension(Sim sim)
    {
        public int GetPosition(IEntity entity) => sim.GetSystem<Spatial1DSystem>().GetState(entity).Position;
    }
}
