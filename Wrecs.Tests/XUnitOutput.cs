namespace Wrecs.Tests;

public class XUnitOutput(ITestOutputHelper output) : IOutput
{
    public void WriteLine(string message) => output.WriteLine(message);
}
