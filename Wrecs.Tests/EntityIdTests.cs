namespace Wrecs.Tests;

[CollectionDefinition(nameof(EntityIdTests), DisableParallelization = true)]
public class EntityIdTestsCollection;

[Collection(nameof(EntityIdTests))]
public class EntityIdTests
{
    [Fact(DisplayName = "Simultaneous ID generations are all unique")]
    public async Task Next_WhenCalledSimultaneously_ReturnsUniqueIds()
    {
        const int count = 512;
        // Use a gate to prevent the tasks from starting until they are all ready
        var gate = new TaskCompletionSource();
        var tasks = Enumerable.Range(0, count)
                              .Select(_ => Task.Run(async () => { await gate.Task; return EntityId.Next(); }))
                              .ToArray();
        gate.SetResult();

        var ids = await Task.WhenAll(tasks);

        ids.Should().OnlyHaveUniqueItems();
    }
}
