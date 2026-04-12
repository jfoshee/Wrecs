using System.Collections.ObjectModel;
using System.Diagnostics;

namespace CommerceSim.Core;

[DebuggerDisplay("Money: {MoneyBalance}, Resources: {ResourceBalance}")]
public record struct CommercialSnapshot : IStateSnapshot<CommercialSystem>
{
    // Internally use empty string for unitless (null) since Dictionary doesn't allow null keys
    private const string UnitlessKey = "";

    public int MoneyBalance { get; init; }
    private readonly IReadOnlyDictionary<string, int>? _inventory;
    public IReadOnlyDictionary<string, int> Inventory => _inventory ?? EmptyInventory;
    private static readonly IReadOnlyDictionary<string, int> EmptyInventory = new Dictionary<string, int>();

    /// <summary>
    /// Gets the balance for unitless resources (backward compatibility).
    /// </summary>
    public int ResourceBalance => GetResourceBalance(null);

    public int GetResourceBalance(string? resourceType) =>
        _inventory?.TryGetValue(resourceType ?? UnitlessKey, out var balance) == true ? balance : 0;

    public CommercialSnapshot(int MoneyBalance = 0, int ResourceBalance = 0)
        : this(MoneyBalance, new Dictionary<string, int> { [UnitlessKey] = ResourceBalance })
    {
    }

    public CommercialSnapshot(int moneyBalance, IReadOnlyDictionary<string, int> inventory)
    {
        MoneyBalance = moneyBalance;
        _inventory = inventory;
    }

    internal CommercialSnapshot(CommercialSystem.CommercialState state)
        : this(state.MoneyBalance, new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(state.Inventory)))
    {
    }

    public bool Equals(CommercialSnapshot other) =>
        MoneyBalance == other.MoneyBalance && InventoryEquals(Inventory, other.Inventory);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(MoneyBalance);
        // Only include non-zero inventory values in hash for consistency with equality
        foreach (var kvp in Inventory.Where(k => k.Value != 0).OrderBy(k => k.Key))
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value);
        }
        return hash.ToHashCode();
    }

    private static bool InventoryEquals(IReadOnlyDictionary<string, int> a, IReadOnlyDictionary<string, int> b)
    {
        // Get all keys from both dictionaries
        var allKeys = a.Keys.Union(b.Keys);
        foreach (var key in allKeys)
        {
            var aValue = a.TryGetValue(key, out var av) ? av : 0;
            var bValue = b.TryGetValue(key, out var bv) ? bv : 0;
            if (aValue != bValue) return false;
        }
        return true;
    }
}
