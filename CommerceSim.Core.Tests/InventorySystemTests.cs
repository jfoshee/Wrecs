namespace CommerceSim.Core.Tests;

public record InventoryEntity(int Id, string Name = "Trader") : IInventoryEntity;

public class InventorySystemTests
{
    [Fact(DisplayName = "Default snapshot has empty inventory")]
    public void DefaultSnapshotHasEmptyInventory()
    {
        var snapshot = new InventorySnapshot([]);

        snapshot.Inventory.Should().BeEmpty();
        snapshot.Should().Be(default(InventorySnapshot));
    }

    [Fact(DisplayName = "GetAmount returns correct amount for each type")]
    public void GetAmountReturnsCorrectAmountForEachType()
    {
        var snapshot = new InventorySnapshot([("gold", 10), ("silver", 5)]);

        snapshot.GetAmount("gold").Should().Be(10);
        snapshot.GetAmount("silver").Should().Be(5);
        snapshot.GetAmount("bronze").Should().Be(0);
    }

    [Fact(DisplayName = "Snapshot normalizes by removing zero amounts")]
    public void SnapshotNormalizesByRemovingZeroAmounts()
    {
        var snapshot = new InventorySnapshot([("gold", 0), ("silver", 5), ("bronze", 0)]);

        snapshot.Inventory.Should().ContainSingle();
        snapshot.Inventory.Should().Equal([("silver", 5)]);
    }

    [Fact(DisplayName = "Snapshot normalizes by sorting types alphabetically")]
    public void SnapshotNormalizesBySortingTypesAlphabetically()
    {
        var snapshot = new InventorySnapshot([("zinc", 3), ("alpha", 10), ("beta", 1)]);

        snapshot.Inventory.Select(i => i.Type).Should().BeInAscendingOrder();
        snapshot.Inventory.Should().Equal([("alpha", 10), ("beta", 1), ("zinc", 3)]);
    }

    [Fact(DisplayName = "Snapshots with same items are equal")]
    public void SnapshotsWithSameItemsAreEqual()
    {
        var a = new InventorySnapshot([("gold", 5), ("silver", 10)]);
        var b = new InventorySnapshot([("gold", 5), ("silver", 10)]);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact(DisplayName = "Snapshots with same items in different order are equal")]
    public void SnapshotsWithSameItemsInDifferentOrderAreEqual()
    {
        var a = new InventorySnapshot([("gold", 5), ("silver", 10)]);
        var b = new InventorySnapshot([("silver", 10), ("gold", 5)]);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact(DisplayName = "Snapshots with different amounts are not equal")]
    public void SnapshotsWithDifferentAmountsAreNotEqual()
    {
        var a = new InventorySnapshot([("gold", 5)]);
        var b = new InventorySnapshot([("gold", 6)]);

        a.Should().NotBe(b);
    }

    [Fact(DisplayName = "Snapshots with different types are not equal")]
    public void SnapshotsWithDifferentTypesAreNotEqual()
    {
        var a = new InventorySnapshot([("gold", 5)]);
        var b = new InventorySnapshot([("silver", 5)]);

        a.Should().NotBe(b);
    }

    [Fact(DisplayName = "Snapshot with only zero amounts equals empty snapshot")]
    public void SnapshotWithOnlyZeroAmountsEqualsEmptySnapshot()
    {
        var withZero = new InventorySnapshot([("gold", 0)]);
        var empty = new InventorySnapshot([]);

        withZero.Should().Be(empty);
        withZero.GetHashCode().Should().Be(empty.GetHashCode());
    }

    [Fact(DisplayName = "Entity with no initial state starts with empty inventory")]
    public void EntityWithNoInitialStateStartsWithEmptyInventory()
    {
        var system = new InventorySystem();
        var entity = new InventoryEntity(1);

        system.InitEntities((entity, null));

        system.GetState(entity).Inventory.Should().BeEmpty();
    }

    [Fact(DisplayName = "Entity initial state sets inventory")]
    public void EntityInitialStateSetsInventory()
    {
        var system = new InventorySystem();
        var entity = new InventoryEntity(1);
        var initial = new InventorySnapshot([("wood", 10), ("stone", 5)]);

        system.InitEntities((entity, initial));

        system.GetState(entity).Should().Be(initial);
    }

    [Fact(DisplayName = "Multiple entities are tracked independently")]
    public void MultipleEntitiesAreTrackedIndependently()
    {
        var system = new InventorySystem();
        var entityA = new InventoryEntity(1);
        var entityB = new InventoryEntity(2);
        var stateA = new InventorySnapshot([("gold", 3)]);
        var stateB = new InventorySnapshot([("silver", 7)]);

        system.InitEntities((entityA, stateA), (entityB, stateB));

        system.GetState(entityA).Should().Be(stateA);
        system.GetState(entityB).Should().Be(stateB);
    }

    [Fact(DisplayName = "Reinitializing clears previous entities")]
    public void ReinitializingClearsPreviousEntities()
    {
        var system = new InventorySystem();
        var entityA = new InventoryEntity(1);
        var entityB = new InventoryEntity(2);

        system.InitEntities((entityA, new InventorySnapshot([("gold", 1)])));
        system.InitEntities((entityB, new InventorySnapshot([("silver", 2)])));

        system.GetEntities().Should().ContainSingle().Which.Should().Be(entityB);
    }

    [Fact(DisplayName = "State snapshot is keyed by entity ID")]
    public void StateSnapshotIsKeyedByEntityId()
    {
        var system = new InventorySystem();
        var entityA = new InventoryEntity(1);
        var entityB = new InventoryEntity(2);
        var stateA = new InventorySnapshot([("wood", 5)]);
        var stateB = new InventorySnapshot([("iron", 3)]);

        system.InitEntities((entityA, stateA), (entityB, stateB));

        var snapshot = system.GetStateSnapshot();
        snapshot[entityA.Id].Should().Be(stateA);
        snapshot[entityB.Id].Should().Be(stateB);
    }

    [Fact(DisplayName = "ApplyUpdates replaces the inventory")]
    public void ApplyUpdatesReplacesTheInventory()
    {
        var system = new InventorySystem();
        var entity = new InventoryEntity(1);

        system.InitEntities((entity, new InventorySnapshot([("wood", 10)])));
        system.ApplyUpdates([new EntityUpdate<InventorySnapshot>(entity, new InventorySnapshot([("stone", 7)]))]);

        system.GetState(entity).GetAmount("stone").Should().Be(7);
        system.GetState(entity).GetAmount("wood").Should().Be(0);
    }

    [Fact(DisplayName = "ApplyUpdates only affects specified entities")]
    public void ApplyUpdatesOnlyAffectsSpecifiedEntities()
    {
        var system = new InventorySystem();
        var entityA = new InventoryEntity(1);
        var entityB = new InventoryEntity(2);
        var stateB = new InventorySnapshot([("gold", 4)]);

        system.InitEntities((entityA, new InventorySnapshot([("wood", 2)])), (entityB, stateB));
        system.ApplyUpdates([new EntityUpdate<InventorySnapshot>(entityA, new InventorySnapshot([]))]);

        system.GetState(entityA).Inventory.Should().BeEmpty();
        system.GetState(entityB).Should().Be(stateB);
    }

    [Fact(DisplayName = "Controller adds items on tick")]
    public void ControllerAddsItemsOnTick()
    {
        var sim = new Sim();
        var inventorySystem = new InventorySystem();
        sim.AddSystem(inventorySystem);
        var entity = new InventoryEntity(1);

        sim.InitEntities((entity, [new InventorySnapshot([("wood", 5)])]));
        sim.InitControllers(new AddItemController("wood", 3));

        sim.Tick();

        inventorySystem.GetState(entity).GetAmount("wood").Should().Be(8);
    }

    private class AddItemController(string type, int qty) : IController<InventorySnapshot>
    {
        public IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> all) => all;
        public InventorySnapshot GetNewState(IEntity entity, InventorySnapshot current)
        {
            var updated = current.Inventory
                .Where(i => i.Type != type)
                .Append((type, current.GetAmount(type) + qty));
            return new InventorySnapshot(updated);
        }
    }
}
