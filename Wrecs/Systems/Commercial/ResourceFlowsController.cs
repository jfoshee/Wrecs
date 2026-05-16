using Wrecs.Core;

namespace Wrecs.Systems.Commercial;

public abstract class ResourceFlowsController<TResourceFlowOrigin>(IEnumerable<TResourceFlowOrigin> flowOrigins,
                                                                   IEnumerable<IResourceFlowPolicy> flowPolicies)
    : IController<InventorySnapshot>, IRequire<InventorySystem>
    where TResourceFlowOrigin : IResourceFlowOrigin
{
    private readonly List<TResourceFlowOrigin> _flowOrigins = [.. flowOrigins];
    private readonly List<IResourceFlowPolicy> _flowPolicies = [.. flowPolicies];

    protected InventorySystem? _inventorySystem;
    private Dictionary<IEntity, List<ResourceFlow>> _approvedFlows = [];

    public void Inject(InventorySystem system) => _inventorySystem = system;

    public IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> allEntities)
    {
        if (_inventorySystem == null)
            throw new InvalidOperationException("Dependencies not injected");

        var context = new FlowContext(allEntities);
        var allFlows = _flowOrigins.SelectMany(origin => origin.CreateFlows(context));

        var pendingFlows = new Dictionary<IEntity, List<ResourceFlow>>();
        foreach (var flow in allFlows)
        {
            var entityFlows = pendingFlows.GetValueOrDefault(flow.Entity, []);
            entityFlows.Add(flow);
            pendingFlows[flow.Entity] = entityFlows;
        }

        _approvedFlows = [];
        foreach (var (entity, flows) in pendingFlows)
        {
            var inventory = _inventorySystem.GetState(entity).Inventory;

            var approved = new List<ResourceFlow>();
            foreach (var flow in flows)
            {
                var proposed = new InventorySnapshot(inventory);
                if (_flowPolicies.Any(policy => !policy.CanExecute(flow, proposed)))
                    continue;
                approved.Add(flow);
            }

            if (approved.Count > 0)
                _approvedFlows[entity] = approved;
        }

        return _approvedFlows.Keys;
    }

    public InventorySnapshot GetNewState(IEntity entity, InventorySnapshot current)
    {
        if (!_approvedFlows.TryGetValue(entity, out var flows))
            return current;
        var inventory = current.Inventory.ToDictionary(i => i.Type, i => i.Amount);
        foreach (var flow in flows)
        {
            var key = flow.ResourceType ?? "";
            inventory[key] = inventory.GetValueOrDefault(key, 0) + flow.SignedAmount;
        }
        return new InventorySnapshot(inventory.Select(kvp => (kvp.Key, kvp.Value)));
    }
}
