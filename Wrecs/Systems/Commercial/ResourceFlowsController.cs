using Wrecs.Core;

namespace Wrecs.Systems.Commercial;

public abstract class ResourceFlowsController<TResourceFlowOrigin>(IEnumerable<TResourceFlowOrigin> flowOrigins,
                                                                   IEnumerable<IResourceFlowPolicy> flowPolicies)
    : IPrepareSharedUpdates, IRequire<InventorySystem>
    where TResourceFlowOrigin : IResourceFlowOrigin
{
    private readonly List<TResourceFlowOrigin> _flowOrigins = [.. flowOrigins];
    private readonly List<IResourceFlowPolicy> _flowPolicies = [.. flowPolicies];

    protected InventorySystem? _inventorySystem;

    public void Inject(InventorySystem system) => _inventorySystem = system;

    public IEnumerable<UpdateSet> PrepareSharedUpdates()
    {
        if (_inventorySystem == null)
            throw new InvalidOperationException("Dependencies not injected");

        var allEntities = _inventorySystem.GetEntities();
        var context = new FlowContext(allEntities);
        var allFlows = _flowOrigins.SelectMany(origin => origin.CreateFlows(context));

        var pendingFlows = new Dictionary<IEntity, List<ResourceFlow>>();
        foreach (var flow in allFlows)
        {
            var entityFlows = pendingFlows.GetValueOrDefault(flow.Entity, []);
            entityFlows.Add(flow);
            pendingFlows[flow.Entity] = entityFlows;
        }

        var updates = new List<IEntityUpdate>();
        foreach (var (entity, flows) in pendingFlows)
        {
            var current = _inventorySystem.GetTypedState(entity);
            var inventory = current.Inventory.ToDictionary(i => i.Type, i => i.Amount);

            var approved = new List<ResourceFlow>();
            foreach (var flow in flows)
            {
                var proposed = new InventorySnapshot(inventory.Select(kvp => (kvp.Key, kvp.Value)));
                if (_flowPolicies.Any(policy => !policy.CanExecute(flow, proposed)))
                    continue;
                approved.Add(flow);
            }

            if (approved.Count == 0)
                continue;

            foreach (var flow in approved)
            {
                var key = flow.ResourceType ?? "";
                inventory[key] = inventory.GetValueOrDefault(key, 0) + flow.SignedAmount;
            }

            updates.Add(new EntityUpdate<InventorySnapshot>(entity, new InventorySnapshot(inventory.Select(kvp => (kvp.Key, kvp.Value)))));
        }

        if (updates.Count > 0)
            yield return new UpdateSet(updates);
    }
}
