using System.Collections.Immutable;
using System.Diagnostics;

namespace CommerceSim.Core;

[DebuggerDisplay("Money: {MoneyBalance}, Resources: {ResourceBalance}")]
public record struct CommercialSnapshot : IStateSnapshot<CommercialSystem>
{
    // Internally use empty string for unitless (null)
    private const string UnitlessKey = "";

    public int MoneyBalance { get; init; }
    private readonly ImmutableArray<(string Type, int Amount)> _inventory;

    /// <summary>
    /// Gets the inventory as an immutable array of (Type, Amount) tuples.
    /// Always sorted by Type and excludes zero amounts.
    /// </summary>
    public ImmutableArray<(string Type, int Amount)> Inventory =>
        _inventory.IsDefault ? [] : _inventory;

    /// <summary>
    /// Gets the balance for unitless resources (backward compatibility).
    /// </summary>
    public int ResourceBalance => GetResourceBalance(null);

    public int GetResourceBalance(string? resourceType)
    {
        var key = resourceType ?? UnitlessKey;
        foreach (var (type, amount) in Inventory)
            if (type == key) return amount;
        return 0;
    }

    public CommercialSnapshot(int MoneyBalance = 0, int ResourceBalance = 0)
    {
        this.MoneyBalance = MoneyBalance;
        _inventory = Normalize([(UnitlessKey, ResourceBalance)]);
    }

    public CommercialSnapshot(int moneyBalance, IEnumerable<(string Type, int Amount)> inventory)
    {
        MoneyBalance = moneyBalance;
        _inventory = Normalize(inventory);
    }

    internal CommercialSnapshot(CommercialSystem.CommercialState state)
    {
        MoneyBalance = state.MoneyBalance;
        _inventory = Normalize([.. state.Inventory.Select(kvp => (kvp.Key, kvp.Value))]);
    }

    internal CommercialSnapshot(int money, Dictionary<string, int> inventory)
        : this(money, inventory.Select(kvp => (kvp.Key, kvp.Value)))
    {
    }

    /// <summary>
    /// Normalizes inventory: filters out zero amounts, sorts by Type.
    /// This enables simple SequenceEqual for equality comparison.
    /// </summary>
    private static ImmutableArray<(string Type, int Amount)> Normalize(
        IEnumerable<(string Type, int Amount)> inventory) =>
        [.. inventory.Where(i => i.Amount != 0).OrderBy(i => i.Type)];

    public bool Equals(CommercialSnapshot other) =>
        MoneyBalance == other.MoneyBalance && Inventory.SequenceEqual(other.Inventory);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(MoneyBalance);
        foreach (var (type, amount) in Inventory)
        {
            hash.Add(type);
            hash.Add(amount);
        }
        return hash.ToHashCode();
    }
}
