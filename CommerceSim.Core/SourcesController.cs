namespace CommerceSim.Core;

/// <summary>
/// A commercial controller that applies grants from one or more <see cref="ISource"/> instances
/// and enforces <see cref="IGrantPolicy"/> rules before applying each grant.
/// </summary>
public class SourcesController(IEnumerable<ISource> sources) : ICommercialController
{
    private readonly List<ISource> _sources = [.. sources];
    private readonly List<IGrantPolicy> _grantPolicies =
    [
        new NoNegativeGrantsPolicy()
    ];

    // Pending grants gathered during GetEntitiesToUpdate, consumed in GetNewState.
    private Dictionary<IEntity, List<Grant>> _pendingGrants = [];

    public SourcesController(params ISource[] sources) : this((IEnumerable<ISource>)sources) { }

    public IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> allEntities)
    {
        var context = new Context(allEntities);
        var allGrants = _sources.SelectMany(s => s.CreateGrants(context));

        _pendingGrants = [];
        foreach (var grant in allGrants)
        {
            var entityGrants = _pendingGrants.GetValueOrDefault(grant.Recipient, []);
            entityGrants.Add(grant);
            _pendingGrants[grant.Recipient] = entityGrants;
        }

        return _pendingGrants.Keys;
    }

    public CommercialSnapshot GetNewState(IEntity entity, CommercialSnapshot currentState)
    {
        if (!_pendingGrants.TryGetValue(entity, out var grants))
            return currentState;

        int money = currentState.MoneyBalance;
        var inventory = currentState.Inventory.ToDictionary(i => i.Type, i => i.Amount);

        foreach (var grant in grants)
        {
            if (_grantPolicies.Any(p => !p.CanExecute(grant)))
                continue;
            money += grant.Money;
            var key = grant.ResourceType ?? "";
            var current = inventory.GetValueOrDefault(key, 0);
            inventory[key] = current + grant.Resources;
        }

        return new CommercialSnapshot(money, inventory.Select(kvp => (kvp.Key, kvp.Value)));
    }
}
