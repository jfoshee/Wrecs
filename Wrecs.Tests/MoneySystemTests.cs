namespace Wrecs.Tests;

public record MoneyEntity(int Id, string Name = "Agent") : IMoneyEntity;

public class MoneySystemTests
{
    [Fact(DisplayName = "Default snapshot has zero balance")]
    public void DefaultSnapshotHasZeroBalance()
    {
        var snapshot = new MoneySnapshot(0);

        snapshot.MoneyBalance.Should().Be(0);
        snapshot.Should().Be(default(MoneySnapshot));
    }

    [Fact(DisplayName = "Snapshots with same value are equal")]
    public void SnapshotsWithSameValueAreEqual()
    {
        var a = new MoneySnapshot(100);
        var b = new MoneySnapshot(100);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact(DisplayName = "Snapshots with different values are not equal")]
    public void SnapshotsWithDifferentValuesAreNotEqual()
    {
        var a = new MoneySnapshot(100);
        var b = new MoneySnapshot(200);

        a.Should().NotBe(b);
    }

    [Fact(DisplayName = "Entity with no initial state starts at zero")]
    public void EntityWithNoInitialStateStartsAtZero()
    {
        var system = new MoneySystem();
        var entity = new MoneyEntity(1);

        system.InitEntities((entity, null));

        system.GetTypedState(entity).MoneyBalance.Should().Be(0);
    }

    [Fact(DisplayName = "Entity initial state sets balance")]
    public void EntityInitialStateSetsBalance()
    {
        var system = new MoneySystem();
        var entity = new MoneyEntity(1);

        system.InitEntities((entity, new MoneySnapshot(500)));

        system.GetTypedState(entity).MoneyBalance.Should().Be(500);
    }

    [Fact(DisplayName = "Multiple entities are tracked independently")]
    public void MultipleEntitiesAreTrackedIndependently()
    {
        var system = new MoneySystem();
        var entityA = new MoneyEntity(1);
        var entityB = new MoneyEntity(2);

        system.InitEntities((entityA, new MoneySnapshot(100)), (entityB, new MoneySnapshot(200)));

        system.GetTypedState(entityA).MoneyBalance.Should().Be(100);
        system.GetTypedState(entityB).MoneyBalance.Should().Be(200);
    }

    [Fact(DisplayName = "Reinitializing clears previous entities")]
    public void ReinitializingClearsPreviousEntities()
    {
        var system = new MoneySystem();
        var entityA = new MoneyEntity(1);
        var entityB = new MoneyEntity(2);

        system.InitEntities((entityA, new MoneySnapshot(100)));
        system.InitEntities((entityB, new MoneySnapshot(50)));

        system.GetEntities().Should().ContainSingle().Which.Should().Be(entityB);
        system.GetTypedState(entityB).MoneyBalance.Should().Be(50);
    }

    [Fact(DisplayName = "State snapshot is keyed by entity ID")]
    public void StateSnapshotIsKeyedByEntityId()
    {
        var system = new MoneySystem();
        var entityA = new MoneyEntity(1);
        var entityB = new MoneyEntity(2);

        system.InitEntities((entityA, new MoneySnapshot(300)), (entityB, new MoneySnapshot(150)));

        var snapshot = system.GetStateSnapshot();
        snapshot[entityA.Id].MoneyBalance.Should().Be(300);
        snapshot[entityB.Id].MoneyBalance.Should().Be(150);
    }

    [Fact(DisplayName = "ApplyUpdates changes the balance")]
    public void ApplyUpdatesChangesTheBalance()
    {
        var system = new MoneySystem();
        var entity = new MoneyEntity(1);

        system.InitEntities((entity, new MoneySnapshot(100)));
        system.ApplyUpdates([new MoneyUpdate(entity, 999)]);

        system.GetTypedState(entity).MoneyBalance.Should().Be(999);
    }

    [Fact(DisplayName = "ApplyUpdates only affects specified entities")]
    public void ApplyUpdatesOnlyAffectsSpecifiedEntities()
    {
        var system = new MoneySystem();
        var entityA = new MoneyEntity(1);
        var entityB = new MoneyEntity(2);

        system.InitEntities((entityA, new MoneySnapshot(100)), (entityB, new MoneySnapshot(200)));
        system.ApplyUpdates([new MoneyUpdate(entityA, 42)]);

        system.GetTypedState(entityA).MoneyBalance.Should().Be(42);
        system.GetTypedState(entityB).MoneyBalance.Should().Be(200);
    }

    [Fact(DisplayName = "Controller increases balance on tick")]
    public void ControllerIncreasesBalanceOnTick()
    {
        var sim = new Sim();
        var moneySystem = new MoneySystem();
        sim.AddSystem(moneySystem);
        var entity = new MoneyEntity(1);

        sim.InitEntities((entity, [new MoneySnapshot(100)]));
        sim.AddSystem(new AddMoneyController(50));

        sim.Tick();

        moneySystem.GetTypedState(entity).MoneyBalance.Should().Be(150);
    }

    private class AddMoneyController(int amount) : ISystemSharedUpdates, IRequire<MoneySystem>
    {
        private MoneySystem? _moneySystem;
        public void Inject(MoneySystem system) => _moneySystem = system;

        public IEnumerable<UpdateSet> PrepareSharedUpdates()
        {
            var entities = _moneySystem!.GetEntities();
            var updates = entities.Select(e =>
            {
                var currentBalance = _moneySystem.GetTypedState(e).MoneyBalance;
                var newBalance = currentBalance + amount;
                return (IEntityUpdate)new MoneyUpdate(e, newBalance);
            });
            yield return new(updates);
        }
    }
}
