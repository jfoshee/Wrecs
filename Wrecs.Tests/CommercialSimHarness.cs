namespace Wrecs.Tests;

class CommercialSim : Sim
{
    public CommercialSim()
    {
        AddSystem(new MoneySystem());
        AddSystem(new InventorySystem());
        AddSystem(new OfferSystem());
    }
}

class CommercialSimHarness
{
    private readonly CommercialSim _sim = new();

    // Simplifies initialization for tests that only care about commercial state.
    // Expects initial state to be a single CommercialSnapshot for each entity, if provided
    public void InitEntities(params (IEntity entity, CommercialSnapshot? initialState)[] initialEntities)
    {
        _sim.InitEntities([.. initialEntities.Select(initialEntity =>
        {
            var snapshots = initialEntity.initialState is { } snapshot
                ? new IStateSnapshot[] { snapshot }
                : [];
            return (initialEntity.entity, snapshots);
        })]);
    }

    public void AddSystem(ISystem system) => _sim.AddSystem(system);
    public void AddSystems(params ISystem[] systems) => _sim.AddSystems(systems);

    public CommercialSnapshot GetCommercialState(IEntity entity) => _sim.GetCommercialState(entity);

    public void Tick() => _sim.Tick();
}
