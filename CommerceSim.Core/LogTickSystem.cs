namespace CommerceSim.Core;

public class LogTickSystem(IOutput output) : ISystem
{
    private int _tickCount = 0;

    public void Tick()
    {
        output.WriteLine($"Tick {_tickCount}");
        _tickCount++;
    }
}
