namespace Wrecs.Tests;

public class EntityIdTests
{
    [Fact(DisplayName = "Simultaneous ID generations are all unique")]
    public async Task Next_WhenCalledSimultaneously_ReturnsUniqueIds()
    {
        const int count = 512;
        var tasks = Enumerable.Range(0, count)
                              .Select(_ => Task.Run(() => EntityId.Next()))
                              .ToArray();
        var ids = await Task.WhenAll(tasks);

        ids.Should().OnlyHaveUniqueItems();
    }
}
