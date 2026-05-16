using Wrecs.Core.Spatial;

namespace Wrecs.Core.Tests.Monopoly;

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

    // Calculated per-tick rent adjustments (positive = receiving rent, negative = paying rent)
    private readonly Dictionary<IEntity, int> _rentAdjustments = [];

    public int Id { get; } = EntityId.Next();
    public string Name => "Monopoly Rent Controller";

    public MonopolyRentController() : this(MonopolyBoard.Properties) { }

    public void Inject(TurnSystem dependency) => _turnSystem = dependency;
    public void Inject(SpatialSystem dependency) => _spatialSystem = dependency;
    public void Inject(MoneySystem dependency) => _moneySystem = dependency;
    public void Inject(InventorySystem dependency) => _inventorySystem = dependency;

    public IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> allEntities)
    {
        _rentAdjustments.Clear();

        // Get current player and their position
        var currentPlayer = _turnSystem.GetCurrentPlayer();
        var playerPosition = _spatialSystem.GetState(currentPlayer).Position;

        // Look up property at that position
        if (playerPosition < 0 || playerPosition >= boardConfig.Length)
            return [];
        var property = boardConfig[playerPosition];
        if (property is null)
            return []; // No property at this position (GO, Jail, etc.)

        // Find who owns this property (search all entities for who has it in inventory)
        IEntity? owner = null;
        foreach (var entity in allEntities)
        {
            if (_inventorySystem.GetState(entity).GetAmount(property.Name) > 0)
            {
                owner = entity;
                break;
            }
        }

        if (owner is RealEstateAgent)
            return []; // Owned by bank, no rent

        if (owner is null || owner == currentPlayer)
            return []; // Unowned or player owns it themselves

        // Calculate rent (simplified: 10% of property price)
        var rent = property.Price / 10;

        // Check if tenant can afford rent
        var tenantMoney = _moneySystem.GetState(currentPlayer).MoneyBalance;
        if (tenantMoney < rent)
            rent = tenantMoney; // Pay what they can

        if (rent <= 0)
            return [];

        // Record adjustments
        _rentAdjustments[currentPlayer] = -rent;    // Tenant pays
        _rentAdjustments[owner] = rent;             // Landlord receives

        return [currentPlayer, owner];
    }

    public MoneySnapshot GetNewState(IEntity entity, MoneySnapshot currentState)
    {
        if (_rentAdjustments.TryGetValue(entity, out var adjustment))
            return new MoneySnapshot(currentState.MoneyBalance + adjustment);
        return currentState;
    }

    public InventorySnapshot GetNewState(IEntity entity, InventorySnapshot currentState) => currentState;
}
