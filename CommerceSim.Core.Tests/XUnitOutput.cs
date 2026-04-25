using Xunit.Abstractions;

namespace CommerceSim.Core.Tests;

public class XUnitOutput(ITestOutputHelper output) : IOutput
{
    public void WriteLine(string message) => output.WriteLine(message);
}
