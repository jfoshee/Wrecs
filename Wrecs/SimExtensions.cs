using Wrecs.Core;

namespace Wrecs;

public static class SimExtensions
{
    extension(Sim sim)
    {
        public void AddSystems(params ISystem[] systems)
        {
            foreach (var system in systems)
            {
                sim.AddSystem(system);
            }
        }
    }
}
