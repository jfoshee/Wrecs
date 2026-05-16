namespace Wrecs.Tests;

public class Canary
{
    [Fact]
    public void Test1()
    {
        (1 + 1).Should().Be(2);
    }
}
