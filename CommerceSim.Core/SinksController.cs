namespace CommerceSim.Core;

/// <summary>
/// A commercial controller that applies charges from one or more <see cref="ISink"/> instances
/// and enforces <see cref="IChargePolicy"/> rules before applying each charge.
/// </summary>
public class SinksController(IEnumerable<ISink> sinks) : ICommercialController
{
    private readonly List<ISink> _sinks = [.. sinks];
    private readonly List<IChargePolicy> _chargePolicies =
    [
        new NoNegativeChargesPolicy(),
        new NoForcingNegativeBalanceChargePolicy()
    ];

    // Pending charges gathered during GetEntitiesToUpdate, consumed in GetNewState.
    private Dictionary<IEntity, List<Charge>> _pendingCharges = [];

    public SinksController(params ISink[] sinks) : this((IEnumerable<ISink>)sinks) { }

    public IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> allEntities)
    {
        var context = new Context(allEntities);
        var allCharges = _sinks.SelectMany(s => s.CreateCharges(context));

        _pendingCharges = [];
        foreach (var charge in allCharges)
        {
            var entityCharges = _pendingCharges.GetValueOrDefault(charge.Payor, []);
            entityCharges.Add(charge);
            _pendingCharges[charge.Payor] = entityCharges;
        }

        return _pendingCharges.Keys;
    }

    public CommercialSnapshot GetNewState(IEntity entity, CommercialSnapshot currentState)
    {
        if (!_pendingCharges.TryGetValue(entity, out var charges))
            return currentState;

        int money = currentState.MoneyBalance;
        var inventory = currentState.Inventory.ToDictionary(i => i.Type, i => i.Amount);

        foreach (var charge in charges)
        {
            var inProgress = new CommercialSnapshot(money, inventory);
            if (_chargePolicies.Any(p => !p.CanExecute(charge, inProgress)))
                continue;
            money -= charge.Money;
            var key = charge.ResourceType ?? "";
            var current = inventory.GetValueOrDefault(key, 0);
            inventory[key] = current - charge.Resources;
        }

        return new CommercialSnapshot(money, inventory);
    }
}
