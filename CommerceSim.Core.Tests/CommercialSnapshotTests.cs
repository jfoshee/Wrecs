namespace CommerceSim.Core.Tests;

public class CommercialSnapshotTests
{
    [Fact]
    public void DefaultConstructor_CreatesEmptySnapshot()
    {
        var snapshot = new CommercialSnapshot();

        snapshot.MoneyBalance.Should().Be(0);
        snapshot.ResourceBalance.Should().Be(0);
        snapshot.Inventory.Should().BeEmpty();
        snapshot.Should().Be(new CommercialSnapshot());
        snapshot.Should().Be((CommercialSnapshot)default);
    }

    [Fact]
    public void Constructor_WithMoneyOnly_SetsMoneyBalance()
    {
        var snapshot = new CommercialSnapshot(MoneyBalance: 100);

        snapshot.MoneyBalance.Should().Be(100);
        snapshot.ResourceBalance.Should().Be(0);
        snapshot.Inventory.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithResources_SetsResourceBalance()
    {
        var snapshot = new CommercialSnapshot(MoneyBalance: 50, ResourceBalance: 25);

        snapshot.MoneyBalance.Should().Be(50);
        snapshot.ResourceBalance.Should().Be(25);
        snapshot.Inventory.Should().HaveCount(1);
    }

    [Fact]
    public void GetResourceBalance_ReturnsZero_ForUnknownType()
    {
        var snapshot = new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 50);

        snapshot.GetResourceBalance("gold").Should().Be(0);
    }

    [Fact]
    public void GetResourceBalance_Null_ReturnsUnitlessBalance()
    {
        var snapshot = new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 50);

        snapshot.GetResourceBalance(null).Should().Be(50);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 50);
        var b = new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 50);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentMoney_AreNotEqual()
    {
        var a = new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 50);
        var b = new CommercialSnapshot(MoneyBalance: 200, ResourceBalance: 50);

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentResources_AreNotEqual()
    {
        var a = new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 50);
        var b = new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 75);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equality_ZeroResources_EqualsEmptyInventory()
    {
        var withZero = new CommercialSnapshot(MoneyBalance: 100, ResourceBalance: 0);
        var empty = new CommercialSnapshot(MoneyBalance: 100);

        withZero.Should().Be(empty);
        withZero.GetHashCode().Should().Be(empty.GetHashCode());
    }

    [Fact]
    public void Equality_DefaultSnapshot_EqualsExplicitZeros()
    {
        var defaultSnapshot = new CommercialSnapshot();
        var explicitZeros = new CommercialSnapshot(MoneyBalance: 0, ResourceBalance: 0);

        defaultSnapshot.Should().Be(explicitZeros);
    }

    [Fact]
    public void Inventory_IsNormalized_SortedByType()
    {
        var snapshot = new CommercialSnapshot(100, [("zinc", 5), ("alpha", 10), ("beta", 3)]);

        snapshot.Inventory.Select(i => i.Type).Should().BeInAscendingOrder();
        snapshot.Inventory.Should().Equal([("alpha", 10), ("beta", 3), ("zinc", 5)]);
    }

    [Fact]
    public void Inventory_IsNormalized_ZerosRemoved()
    {
        var snapshot = new CommercialSnapshot(100, [("gold", 0), ("silver", 10), ("bronze", 0)]);

        snapshot.Inventory.Should().HaveCount(1);
        snapshot.Inventory.Should().Equal([("silver", 10)]);
    }

    [Fact]
    public void Equality_SameTypedResources_DifferentOrder_AreEqual()
    {
        var a = new CommercialSnapshot(100, [("gold", 5), ("silver", 10)]);
        var b = new CommercialSnapshot(100, [("silver", 10), ("gold", 5)]);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentTypedResources_AreNotEqual()
    {
        var a = new CommercialSnapshot(100, [("gold", 5)]);
        var b = new CommercialSnapshot(100, [("silver", 5)]);

        a.Should().NotBe(b);
    }

    [Fact]
    public void GetResourceBalance_TypedResource_ReturnsCorrectAmount()
    {
        var snapshot = new CommercialSnapshot(100, [("gold", 5), ("silver", 10)]);

        snapshot.GetResourceBalance("gold").Should().Be(5);
        snapshot.GetResourceBalance("silver").Should().Be(10);
        snapshot.GetResourceBalance("bronze").Should().Be(0);
    }

    [Fact]
    public void Equality_MixedUnitlessAndTyped_WorksCorrectly()
    {
        var a = new CommercialSnapshot(100, [("", 50), ("gold", 5)]);
        var b = new CommercialSnapshot(100, [("gold", 5), ("", 50)]);

        a.Should().Be(b);
        a.ResourceBalance.Should().Be(50);
        b.ResourceBalance.Should().Be(50);
    }
}
