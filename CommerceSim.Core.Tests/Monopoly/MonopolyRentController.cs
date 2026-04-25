using CommerceSim.Core.Spatial;

namespace CommerceSim.Core.Tests.Monopoly;

/// <summary>
/// Responsible for charging rent by taking money from players who land on properties owned by other players
/// and giving the money to the owning player.
/// </summary>
public class MonopolyRentController(MonopolyProperty?[] boardConfig)
    : ICommercialController, IRequire<TurnSystem>, IRequire<SpatialSystem>, IRequire<CommercialSystem>
{
    private TurnSystem _turnSystem = null!;
    private SpatialSystem _spatialSystem = null!;
    private CommercialSystem _commercialSystem = null!;

    // Calculated per-tick rent adjustments (positive = receiving rent, negative = paying rent)
    private readonly Dictionary<IEntity, int> _rentAdjustments = [];

    public int Id { get; } = EntityId.Next();
    public string Name => "Monopoly Rent Controller";

    public MonopolyRentController() : this(MonopolyBoard.Properties) { }

    public void Inject(TurnSystem dependency) => _turnSystem = dependency;
    public void Inject(SpatialSystem dependency) => _spatialSystem = dependency;
    public void Inject(CommercialSystem dependency) => _commercialSystem = dependency;

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
            var state = _commercialSystem.GetState(entity);
            if (state.GetResourceBalance(property.Name) > 0)
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
        var tenantState = _commercialSystem.GetState(currentPlayer);
        if (tenantState.MoneyBalance < rent)
            rent = tenantState.MoneyBalance; // Pay what they can

        if (rent <= 0)
            return [];

        // Record adjustments
        _rentAdjustments[currentPlayer] = -rent;    // Tenant pays
        _rentAdjustments[owner] = rent;             // Landlord receives

        return [currentPlayer, owner];
    }

    public CommercialSnapshot GetNewState(IEntity entity, CommercialSnapshot currentState)
    {
        if (_rentAdjustments.TryGetValue(entity, out var adjustment))
        {
            return currentState with { MoneyBalance = currentState.MoneyBalance + adjustment };
        }
        return currentState;
    }
}
