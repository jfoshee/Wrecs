namespace CommerceSim.Core.Tests;

public class TurnSystemTests
{
    private record TestTurnEntity(int Id, string Name) : ITakeTurns;

    [Fact(DisplayName = "Single entity is always current")]
    public void SingleEntityIsAlwaysCurrent()
    {
        var system = new TurnSystem();
        var entity = new TestTurnEntity(1, "Entity1");
        system.InitEntities((entity, null));

        var state = system.GetState(entity);

        state.IsMyTurn.Should().BeTrue();
    }

    [Fact(DisplayName = "First entity is current by default")]
    public void FirstEntityIsCurrentByDefault()
    {
        var system = new TurnSystem();
        var entity1 = new TestTurnEntity(1, "Entity1");
        var entity2 = new TestTurnEntity(2, "Entity2");
        system.InitEntities((entity1, null), (entity2, null));

        system.GetState(entity1).IsMyTurn.Should().BeTrue();
        system.GetState(entity2).IsMyTurn.Should().BeFalse();
    }

    [Fact(DisplayName = "Initial state sets current turn")]
    public void InitialStateSetsCurrentTurn()
    {
        var system = new TurnSystem();
        var entity1 = new TestTurnEntity(1, "Entity1");
        var entity2 = new TestTurnEntity(2, "Entity2");
        system.InitEntities((entity1, new TurnSnapshot(false)), (entity2, new TurnSnapshot(true)));

        system.GetState(entity1).IsMyTurn.Should().BeFalse();
        system.GetState(entity2).IsMyTurn.Should().BeTrue();
    }

    [Fact(DisplayName = "Tick advances to next entity")]
    public void TickAdvancesToNextEntity()
    {
        var system = new TurnSystem();
        var entity1 = new TestTurnEntity(1, "Entity1");
        var entity2 = new TestTurnEntity(2, "Entity2");
        system.InitEntities((entity1, null), (entity2, null));

        system.Tick();

        system.GetState(entity1).IsMyTurn.Should().BeFalse();
        system.GetState(entity2).IsMyTurn.Should().BeTrue();
    }

    [Fact(DisplayName = "Tick wraps around to first entity")]
    public void TickWrapsAroundToFirstEntity()
    {
        var system = new TurnSystem();
        var entity1 = new TestTurnEntity(1, "Entity1");
        var entity2 = new TestTurnEntity(2, "Entity2");
        system.InitEntities((entity1, null), (entity2, null));

        system.Tick(); // entity2's turn
        system.Tick(); // wrap back to entity1

        system.GetState(entity1).IsMyTurn.Should().BeTrue();
        system.GetState(entity2).IsMyTurn.Should().BeFalse();
    }

    [Fact(DisplayName = "Multiple ticks cycle through all entities")]
    public void MultipleTicksCycleThroughAllEntities()
    {
        var system = new TurnSystem();
        var entity1 = new TestTurnEntity(1, "Entity1");
        var entity2 = new TestTurnEntity(2, "Entity2");
        var entity3 = new TestTurnEntity(3, "Entity3");
        system.InitEntities((entity1, null), (entity2, null), (entity3, null));

        // Initial state - entity1's turn
        system.GetState(entity1).IsMyTurn.Should().BeTrue();

        system.Tick(); // entity2's turn
        system.GetState(entity2).IsMyTurn.Should().BeTrue();

        system.Tick(); // entity3's turn
        system.GetState(entity3).IsMyTurn.Should().BeTrue();

        system.Tick(); // wrap back to entity1
        system.GetState(entity1).IsMyTurn.Should().BeTrue();
    }

    [Fact(DisplayName = "Only one entity has turn at a time")]
    public void OnlyOneEntityHasTurnAtATime()
    {
        var system = new TurnSystem();
        var entities = Enumerable.Range(1, 5)
            .Select(i => new TestTurnEntity(i, $"Entity{i}"))
            .ToArray();
        system.InitEntities([.. entities.Select(e => ((IEntity)e, (TurnSnapshot?)null))]);

        for (int tick = 0; tick < 10; tick++)
        {
            var entitiesWithTurn = entities.Where(e => system.GetState(e).IsMyTurn).ToList();
            entitiesWithTurn.Should().HaveCount(1);
            system.Tick();
        }
    }

    [Fact(DisplayName = "Single entity turn persists after tick")]
    public void SingleEntityTurnPersistsAfterTick()
    {
        var system = new TurnSystem();
        var entity = new TestTurnEntity(1, "Entity1");
        system.InitEntities((entity, null));

        system.Tick();

        system.GetState(entity).IsMyTurn.Should().BeTrue();
    }
}
