using Wrecs.Core;
using Wrecs.Systems;

namespace Wrecs.Tests.Monopoly;

/// <summary>
/// Responsible for charging rent by taking money from players who land on properties owned by other players
/// and giving the money to the owning player.
/// </summary>
public class MonopolyRentController(MonopolyProperty?[] boardConfig)
    : ICommercialController, IRequire<TurnSystem>, IRequire<SpatialSystem>,
      IRequire<MoneySystem>, IRequire<InventorySystem>
{
    private TurnSystem _turnSystem = null!;
    private SpatialSystem _spatialSystem = null!;
    private MoneySystem _moneySystem = null!;
    private InventorySystem _inventorySystem = null!;

    public MonopolyRentController() : this(MonopolyBoard.Properties) { }

    public void Inject(TurnSystem dependency) => _turnSystem = dependency;
    public void Inject(SpatialSystem dependency) => _spatialSystem = dependency;
    public void Inject(MoneySystem dependency) => _moneySystem = dependency;
    public void Inject(InventorySystem dependency) => _inventorySystem = dependency;

    public IEnumerable<UpdateSet> PrepareSharedUpdates()
    {
        // Get current player and their position
        var currentPlayer = _turnSystem.GetCurrentPlayer();
        var playerPosition = _spatialSystem.GetState(currentPlayer).Position;

        // Look up property at that position
        if (playerPosition < 0 || playerPosition >= boardConfig.Length)
            yield break;
        var property = boardConfig[playerPosition];
        if (property is null)
            yield break; // No property at this position (GO, Jail, etc.)

        // Find who owns this property (search all entities for who has it in inventory)
        IEntity? owner = null;
        foreach (var entity in _inventorySystem.GetEntities())
        {
            if (_inventorySystem.GetState(entity).GetAmount(property.Name) > 0)
            {
                owner = entity;
                break;
            }
        }

        if (owner is RealEstateAgent)
            yield break; // Owned by bank, no rent

        if (owner is null || owner == currentPlayer)
            yield break; // Unowned or player owns it themselves

        // Calculate rent (simplified: 10% of property price)
        var rent = property.Price / 10;

        // Check if tenant can afford rent
        var tenantMoney = _moneySystem.GetState(currentPlayer).MoneyBalance;
        if (tenantMoney < rent)
            rent = tenantMoney; // Pay what they can

        if (rent <= 0)
            yield break;

        var tenantNewBalance = tenantMoney - rent;  // Tenant pays
        var ownerNewBalance = _moneySystem.GetState(owner).MoneyBalance + rent;  // Landlord receives

        yield return new UpdateSet([
            new EntityUpdate<MoneySnapshot>(currentPlayer, new MoneySnapshot(tenantNewBalance)),
            new EntityUpdate<MoneySnapshot>(owner, new MoneySnapshot(ownerNewBalance)),
        ]);
    }
}
