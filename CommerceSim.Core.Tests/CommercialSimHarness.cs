namespace CommerceSim.Core.Tests;

class CommercialSimHarness
{
    private readonly Sim _sim = new();

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

    public void InitControllers(params IController[] controllers) => _sim.InitControllers(controllers);

    public CommercialSnapshot GetCommercialState(IEntity entity) => _sim.GetCommercialState(entity);

    public void Tick() => _sim.Tick();
}
