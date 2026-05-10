namespace CommerceSim.Core.Tests;

public class CommercialSnapshotTests
{
    [Fact(DisplayName = "Default snapshot has zero money and empty inventory")]
    public void DefaultSnapshotHasZeroMoneyAndEmptyInventory()
    {
        var snapshot = new CommercialSnapshot();

        snapshot.MoneyBalance.Should().Be(0);
        snapshot.ResourceBalance.Should().Be(0);
        snapshot.Inventory.Inventory.Should().BeEmpty();
        snapshot.Should().Be(new CommercialSnapshot());
        snapshot.Should().Be((CommercialSnapshot)default);
    }

    [Fact(DisplayName = "Snapshot with money and resource balance sets both correctly")]
    public void SnapshotWithMoneyAndResourceBalanceSetsBothCorrectly()
    {
        var snapshot = new CommercialSnapshot(MoneyBalance: 50, ResourceBalance: 25);

        snapshot.MoneyBalance.Should().Be(50);
        snapshot.ResourceBalance.Should().Be(25);
        snapshot.Inventory.Inventory.Should().HaveCount(1);
    }

    [Fact(DisplayName = "Snapshot with money only has zero resource balance")]
    public void SnapshotWithMoneyOnlyHasZeroResourceBalance()
    {
        var snapshot = new CommercialSnapshot(MoneyBalance: 100);

        snapshot.MoneyBalance.Should().Be(100);
        snapshot.ResourceBalance.Should().Be(0);
        snapshot.Inventory.Inventory.Should().BeEmpty();
    }

    [Fact(DisplayName = "Snapshot with typed inventory sets typed balances")]
    public void SnapshotWithTypedInventorySetsTypedBalances()
    {
        var snapshot = new CommercialSnapshot(100, [("gold", 5), ("silver", 10)]);

        snapshot.MoneyBalance.Should().Be(100);
        snapshot.GetResourceBalance("gold").Should().Be(5);
        snapshot.GetResourceBalance("silver").Should().Be(10);
        snapshot.ResourceBalance.Should().Be(0);
    }

    [Fact(DisplayName = "GetResourceBalance returns zero for unknown type")]
    public void GetResourceBalanceReturnsZeroForUnknownType()
    {
        var snapshot = new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 50);

        snapshot.GetResourceBalance("gold").Should().Be(0);
    }

    [Fact(DisplayName = "GetResourceBalance with null returns unitless balance")]
    public void GetResourceBalanceWithNullReturnsUnitlessBalance()
    {
        var snapshot = new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 50);

        snapshot.GetResourceBalance(null).Should().Be(50);
    }

    [Fact(DisplayName = "Equal snapshots have same value and hash code")]
    public void EqualSnapshotsHaveSameValueAndHashCode()
    {
        var a = new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 50);
        var b = new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 50);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact(DisplayName = "Different money balance makes snapshots unequal")]
    public void DifferentMoneyMakesSnapshotsUnequal()
    {
        var a = new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 50);
        var b = new CommercialSnapshot(MoneyBalance: 200, ResourceBalance: 50);

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Fact(DisplayName = "Different resource balance makes snapshots unequal")]
    public void DifferentResourceBalanceMakesSnapshotsUnequal()
    {
        var a = new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 50);
        var b = new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 75);

        a.Should().NotBe(b);
    }

    [Fact(DisplayName = "Zero resource balance equals empty inventory snapshot")]
    public void ZeroResourceBalanceEqualsEmptyInventorySnapshot()
    {
        var withZero = new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0);
        var empty = new CommercialSnapshot(MoneyBalance: 100);

        withZero.Should().Be(empty);
        withZero.GetHashCode().Should().Be(empty.GetHashCode());
    }

    [Fact(DisplayName = "Default snapshot equals explicit zero snapshot")]
    public void DefaultSnapshotEqualsExplicitZeroSnapshot()
    {
        var defaultSnapshot = new CommercialSnapshot();
        var explicitZeros = new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 0);

        defaultSnapshot.Should().Be(explicitZeros);
    }

    [Fact(DisplayName = "Typed inventory is sorted alphabetically by type")]
    public void TypedInventoryIsSortedAlphabeticallyByType()
    {
        var snapshot = new CommercialSnapshot(100, [("zinc", 5), ("alpha", 10), ("beta", 3)]);

        snapshot.Inventory.Inventory.Select(i => i.Type).Should().BeInAscendingOrder();
        snapshot.Inventory.Inventory.Should().Equal([("alpha", 10), ("beta", 3), ("zinc", 5)]);
    }

    [Fact(DisplayName = "Typed inventory with zero amounts are excluded")]
    public void TypedInventoryWithZeroAmountsAreExcluded()
    {
        var snapshot = new CommercialSnapshot(100, [("gold", 0), ("silver", 10), ("bronze", 0)]);

        snapshot.Inventory.Inventory.Should().HaveCount(1);
        snapshot.Inventory.Inventory.Should().Equal([("silver", 10)]);
    }

    [Fact(DisplayName = "Same typed resources in different order produce equal snapshots")]
    public void SameTypedResourcesInDifferentOrderProduceEqualSnapshots()
    {
        var a = new CommercialSnapshot(100, [("gold", 5), ("silver", 10)]);
        var b = new CommercialSnapshot(100, [("silver", 10), ("gold", 5)]);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact(DisplayName = "Different typed resource types make snapshots unequal")]
    public void DifferentTypedResourceTypesMakeSnapshotsUnequal()
    {
        var a = new CommercialSnapshot(100, [("gold", 5)]);
        var b = new CommercialSnapshot(100, [("silver", 5)]);

        a.Should().NotBe(b);
    }

    [Fact(DisplayName = "Unitless and typed resources coexist independently")]
    public void UnitlessAndTypedResourcesCoexistIndependently()
    {
        var a = new CommercialSnapshot(100, [("", 50), ("gold", 5)]);
        var b = new CommercialSnapshot(100, [("gold", 5), ("", 50)]);

        a.Should().Be(b);
        a.ResourceBalance.Should().Be(50);
        b.ResourceBalance.Should().Be(50);
        a.GetResourceBalance("gold").Should().Be(5);
    }
}
