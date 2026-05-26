using Wrecs.Core;

namespace Wrecs.Systems.Commercial;

public record struct CommercialSnapshot(MoneySnapshot Money, InventorySnapshot Inventory) : IStateSnapshot
{
    public CommercialSnapshot(int MoneyBalance, int ResourceBalance = 0)
        : this(new MoneySnapshot(MoneyBalance), new InventorySnapshot([("", ResourceBalance)])) { }

    public CommercialSnapshot(int MoneyBalance, IEnumerable<(string Type, int Amount)> inventory)
        : this(new MoneySnapshot(MoneyBalance), new InventorySnapshot(inventory)) { }

    public readonly int MoneyBalance => Money.MoneyBalance;
    public readonly int ResourceBalance => Inventory.GetAmount("");
    public readonly int GetResourceBalance(string? resourceType) => Inventory.GetAmount(resourceType ?? "");
}

public interface ICommercialEntity : IMoneyEntity, IInventoryEntity;
