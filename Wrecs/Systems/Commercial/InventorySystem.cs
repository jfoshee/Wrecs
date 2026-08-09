using System.Collections.Immutable;
using Wrecs.Core;

namespace Wrecs.Systems.Commercial;

public interface IInventoryEntity : IEntity;

public readonly record struct InventorySnapshot : IStateSnapshot<InventorySystem>
{
    private readonly ImmutableArray<(string Type, int Amount)> _inventory;

    public readonly ImmutableArray<(string Type, int Amount)> Inventory =>
        _inventory.IsDefault ? [] : _inventory;

    public InventorySnapshot(IEnumerable<(string Type, int Amount)> inventory)
    {
        _inventory = Normalize(inventory);
    }

    public readonly int GetAmount(string resourceType)
    {
        foreach (var (type, amount) in Inventory)
            if (type == resourceType) return amount;
        return 0;
    }

    private static ImmutableArray<(string Type, int Amount)> Normalize(
        IEnumerable<(string Type, int Amount)> inventory) =>
        [.. inventory.Where(i => i.Amount != 0).OrderBy(i => i.Type)];

    public readonly bool Equals(InventorySnapshot other) => Inventory.SequenceEqual(other.Inventory);

    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var (type, amount) in Inventory)
        {
            hash.Add(type);
            hash.Add(amount);
        }
        return hash.ToHashCode();
    }

    public override readonly string ToString() =>
        string.Join(", ", Inventory.Select(i => $"{i.Type}: {i.Amount}"));
}

public record InventoryUpdate : EntityUpdate<InventorySnapshot>
{
    public InventoryUpdate(IEntity entity, params (string Type, int Amount)[] inventory)
        : base(entity, new InventorySnapshot(inventory))
    {
    }
}

public class InventorySystem :
    ISystemWithDynamicEntities<IInventoryEntity, InventorySnapshot>,
    ISystemUpdateAcceptor<InventorySnapshot>,
    ISystemAgentContextProvider<InventorySnapshot>
{
    private readonly List<IEntity> _entities = [];
    private readonly Dictionary<IEntity, Dictionary<string, int>> _inventories = [];

    public InventorySnapshot GetTypedState(IEntity entity) =>
        new(_inventories[entity].Select(kvp => (kvp.Key, kvp.Value)));

    public IReadOnlyList<IEntity> GetEntities() => _entities;

    public IReadOnlyDictionary<int, InventorySnapshot> GetStateSnapshot() =>
        _inventories.ToDictionary(kvp => kvp.Key.Id, kvp =>
            new InventorySnapshot(kvp.Value.Select(i => (i.Key, i.Value))));

    public void InitEntities(params (IEntity entity, InventorySnapshot? initialState)[] initialEntities)
    {
        _entities.Clear();
        _inventories.Clear();
        foreach (var (entity, initialState) in initialEntities)
            AddEntity(entity, initialState);
    }

    public void AddEntity(IEntity entity, InventorySnapshot? initialState)
    {
        _entities.Add(entity);
        var inventory = new Dictionary<string, int>();
        if (initialState.HasValue)
        {
            foreach (var (type, amount) in initialState.Value.Inventory)
                inventory[type] = amount;
        }
        _inventories[entity] = inventory;
    }

    bool ISystemWithEntityStateSnapshots.HasInitialState(IEnumerable<IStateSnapshot> initialStates) =>
        initialStates.Any(initialState => initialState is InventorySnapshot or CommercialSnapshot);

    void ISystemEntityStateAdder.AddEntity(IEntity entity, IStateSnapshot[] initialStates)
    {
        var initialState = initialStates
            .OfType<InventorySnapshot>()
            .Select(state => (InventorySnapshot?)state)
            .FirstOrDefault()
            ?? initialStates
                .OfType<CommercialSnapshot>()
                .Select(state => (InventorySnapshot?)state.Inventory)
                .FirstOrDefault();

        AddEntity(entity, initialState);
    }

    void ISystemEntityStateInitializer.InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState)
    {
        var initialEntities = entitiesWithState
            .Select(e => (e.entity, initialState:
                e.initialStates.OfType<InventorySnapshot>().Select(s => (InventorySnapshot?)s).FirstOrDefault()
                ?? e.initialStates.OfType<CommercialSnapshot>().Select(s => (InventorySnapshot?)s.Inventory).FirstOrDefault()))
            .ToArray();
        InitEntities(initialEntities);
    }

    public InventorySnapshot? BuildSnapshot(IAgent agent) =>
        _entities.Contains(agent) ? GetTypedState(agent) : null;

    public void ApplyUpdates(IEnumerable<EntityUpdate<InventorySnapshot>> updates)
    {
        foreach (var update in updates)
        {
            var inv = _inventories[update.Entity];
            inv.Clear();
            foreach (var (type, amount) in update.State.Inventory)
                inv[type] = amount;
        }
    }
}
