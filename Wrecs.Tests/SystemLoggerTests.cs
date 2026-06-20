using Wrecs.Systems;

namespace Wrecs.Tests;

public class SystemLoggerTests
{
    private record TestTurnEntity(int Id, string Name) : ITakeTurns;

    private sealed class CollectingOutput : IOutput
    {
        public List<string> Lines { get; } = [];

        public void WriteLine(string message) => Lines.Add(message);
    }

    [Fact(DisplayName = "System logger writes each entity snapshot for the target system")]
    public void SystemLoggerWritesEachEntitySnapshot()
    {
        var output = new CollectingOutput();
        var system = new TurnSystem();
        var logger = new SystemLogger<TurnSystem>(output);
        var entity1 = new TestTurnEntity(1, "Entity1");
        var entity2 = new TestTurnEntity(2, "Entity2");

        logger.Inject(system);
        system.InitEntities((entity1, null), (entity2, null));

        // Prepare phase
        logger.PrepareInternalUpdates();
        // Apply phase
        logger.ApplyInternalUpdates();
        system.ApplyInternalUpdates();

        output.Lines.Should().Equal(
            "--- TurnSystem Tick 0 ---",
            "Entity1 (1): TurnSnapshot { IsMyTurn = True, Phase = 0 }",
            "Entity2 (2): TurnSnapshot { IsMyTurn = False, Phase = 0 }");
    }

    [Fact(DisplayName = "System logger piggy-backs on Sim tick order")]
    public void SystemLoggerPiggyBacksOnSimTick()
    {
        var output = new CollectingOutput();
        var sim = new Sim();
        var turnSystem = new TurnSystem();
        var logger = new SystemLogger<TurnSystem>(output);
        var entity1 = new TestTurnEntity(1, "Entity1");
        var entity2 = new TestTurnEntity(2, "Entity2");

        sim.AddSystem(turnSystem);
        sim.AddSystem(logger);
        sim.InitEntities((entity1, []), (entity2, []));

        sim.Tick();

        output.Lines.Should().Equal(
            "--- TurnSystem Tick 0 ---",
            "Entity1 (1): TurnSnapshot { IsMyTurn = True, Phase = 0 }",
            "Entity2 (2): TurnSnapshot { IsMyTurn = False, Phase = 0 }");
    }
}
