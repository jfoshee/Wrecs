using Wrecs.Systems;

namespace Wrecs.Tests.Monopoly;

public interface IMonopolyEntity : ISpatialEntity, ITakeTurns, ICommercialEntity;

public class MonopolyGame : Sim
{
    private MonopolyJailSystem JailSystem { get; } = new();
    private readonly IOutput output;

    // requires 1 spatial tick = 1 turn = 1 commercial tick
    public IMonopolyEntity Player1 { get; internal set; }
    public IMonopolyEntity Player2 { get; }
    public RealEstateAgent RealEstateAgent { get; } = new();

    public MonopolyGame() : this(null)
    {
    }

    public MonopolyGame(IOutput? output)
    {
        this.output = output ?? new NullOutput();

        // 3 Phases per turn: Movement, Offer, Decision
        AddSystem(new TurnSystem(phasesPerTurn: 3));
        AddSystem(JailSystem);
        AddSystem(new LogTickSystem(this.output));
        AddSystem(new SystemLogger<TurnSystem>(this.output));
        AddSystem(new SystemLogger<MoneySystem>(this.output));
        AddSystem(new SystemLogger<InventorySystem>(this.output));
        AddSystem(new SystemLogger<OfferSystem>(this.output));
        AddSystem(new SystemLogger<SpatialSystem>(this.output));
        AddSystem(new SystemLogger<MonopolyJailSystem>(this.output));

        Player1 = new MonopolyPlayer("Player 1");
        Player2 = new MonopolyPlayer("Player 2");
    }

    public void Init(IGameDice? dice = null)
    {
        dice ??= new GameDice(Count: 2);
        var startingMoney = new CommercialSnapshot(MoneyBalance: 1500, 0);
        var allProperties = new CommercialSnapshot(0, MonopolyBoard.Properties.OfType<MonopolyProperty>().Select(p => (p.Name, 1)));
        InitControllers(
            new MonopolyRentController(),
            new BoardGameMovementController(dice, boardSize: 40),
            new MonopolyJailController()
        );
        InitEntities(
            (RealEstateAgent, [allProperties]),
            (new JailerAgent(), [new CommercialSnapshot(0, [(MonopolyJailSystem.PayFineResource, int.MaxValue)])]), // Jailer seeded with infinite "Pay Fine" resource to sell to inmates
            (Player1, [startingMoney]),
            (Player2, [startingMoney])
        );
    }

    internal MonopolyJailSnapshot GetJailState(IEntity entity) => JailSystem.GetState(entity);
}
