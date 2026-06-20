using Wrecs.Core;

namespace Wrecs.Systems;

public class LogTickSystem(IOutput output) : ISystemWithInternalUpdates
{
    private int _tickCount = 0;

    public void PrepareInternalUpdates()
    {
        output.WriteLine($"=== Tick {_tickCount} ===");
    }

    public void ApplyInternalUpdates()
    {
        _tickCount++;
    }
}
