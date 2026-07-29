namespace Wrecs.Tests;

public record TestEntity(int Id, string Name = "Test") : IEntity;
public record struct StateA(int Value) : IStateSnapshot<SystemA>;
public record struct StateB(string Data) : IStateSnapshot<SystemB>;

public class SystemA : ISystemWithEntities<TestEntity, StateA>, ISystemUpdateAcceptor<StateA>
{
    private readonly List<IEntity> _entities = [];
    private readonly Dictionary<IEntity, StateA> _states = [];

    public void InitEntities(params (IEntity entity, StateA? initialState)[] initialEntities)
    {
        _entities.Clear();
        foreach (var (entity, initialState) in initialEntities)
        {
            _entities.Add(entity);
            if (initialState.HasValue) _states[entity] = initialState.Value;
        }
    }

    public IReadOnlyList<IEntity> GetEntities() => _entities;
    public StateA GetTypedState(IEntity entity) => _states.TryGetValue(entity, out var s) ? s : default;
    public void ApplyUpdates(IEnumerable<EntityUpdate<StateA>> updates)
    {
        foreach (var update in updates)
            _states[update.Entity] = update.State;
    }
}

public class SystemB : ISystemWithEntities<TestEntity, StateB>, ISystemUpdateAcceptor<StateB>
{
    private readonly List<IEntity> _entities = [];
    private readonly Dictionary<IEntity, StateB> _states = [];

    public void InitEntities(params (IEntity entity, StateB? initialState)[] initialEntities)
    {
        _entities.Clear();
        foreach (var (entity, initialState) in initialEntities)
        {
            _entities.Add(entity);
            if (initialState.HasValue) _states[entity] = initialState.Value;
        }
    }

    public IReadOnlyList<IEntity> GetEntities() => _entities;
    public StateB GetTypedState(IEntity entity) => _states.TryGetValue(entity, out var s) ? s : default;
    public void ApplyUpdates(IEnumerable<EntityUpdate<StateB>> updates)
    {
        foreach (var update in updates)
            _states[update.Entity] = update.State;
    }
}

public class ControllerA : ISystemUpdateProposer, IRequire<SystemA>
{
    private SystemA? _systemA;
    public void Inject(SystemA system) => _systemA = system;

    public int ProposeUpdatesCalls { get; private set; }

    public IEnumerable<UpdateSet> ProposeUpdates()
    {
        ProposeUpdatesCalls++;
        var updates = _systemA!.GetEntities()
            .Select(e => (IEntityUpdate)new EntityUpdate<StateA>(e, new(_systemA.GetTypedState(e).Value + 1)));
        yield return new(updates);
    }
}

public class ControllerB : ISystemUpdateProposer, IRequire<SystemB>
{
    private SystemB? _systemB;
    public void Inject(SystemB system) => _systemB = system;

    public int ProposeUpdatesCalls { get; private set; }

    public IEnumerable<UpdateSet> ProposeUpdates()
    {
        ProposeUpdatesCalls++;
        var updates = _systemB!.GetEntities()
            .Select(e => (IEntityUpdate)new EntityUpdate<StateB>(e, new(_systemB.GetTypedState(e).Data + "B")));
        yield return new(updates);
    }
}

public class CombinedController : ISystemUpdateProposer, IRequire<SystemA>, IRequire<SystemB>
{
    private SystemA? _systemA;
    private SystemB? _systemB;
    public void Inject(SystemA system) => _systemA = system;
    public void Inject(SystemB system) => _systemB = system;

    public int ProposeUpdatesCalls { get; private set; }

    public IEnumerable<UpdateSet> ProposeUpdates()
    {
        ProposeUpdatesCalls++;
        var updatesA = _systemA!.GetEntities()
            .Select(e => (IEntityUpdate)new EntityUpdate<StateA>(e, new(_systemA.GetTypedState(e).Value + 10)));
        var updatesB = _systemB!.GetEntities()
            .Select(e => (IEntityUpdate)new EntityUpdate<StateB>(e, new(_systemB.GetTypedState(e).Data + "C")));
        yield return new(updatesA.Concat(updatesB));
    }
}

public class ControllerTests
{
    [Fact(DisplayName = "Controllers can impact system state")]
    public void StandardControllers_CanImpactSystemState()
    {
        var sim = new Sim();
        var systemA = new SystemA();
        var systemB = new SystemB();
        sim.AddSystem(systemA);
        sim.AddSystem(systemB);

        var entity = new TestEntity(1);
        sim.InitEntities((entity, [new StateA(10), new StateB("Test")]));

        var controllerA = new ControllerA();
        var controllerB = new ControllerB();
        sim.AddSystems(controllerA, controllerB);

        sim.Tick();

        systemA.GetTypedState(entity).Value.Should().Be(11);
        systemB.GetTypedState(entity).Data.Should().Be("TestB");

        controllerA.ProposeUpdatesCalls.Should().Be(1);
        controllerB.ProposeUpdatesCalls.Should().Be(1);
    }

    [Fact(DisplayName = "A controller that impacts multiple systems should only be called once per tick")]
    public void CombinedController_ImpactsMultipleSystems_And_IsCalledOncePerTick()
    {
        var sim = new Sim();
        var systemA = new SystemA();
        var systemB = new SystemB();
        sim.AddSystem(systemA);
        sim.AddSystem(systemB);

        var entity = new TestEntity(1);
        sim.InitEntities((entity, [new StateA(10), new StateB("Test")]));

        var combinedController = new CombinedController();
        sim.AddSystem(combinedController);

        sim.Tick();

        // Verify state changed in both systems
        systemA.GetTypedState(entity).Value.Should().Be(20);
        systemB.GetTypedState(entity).Data.Should().Be("TestC");

        // Key behavior: Should only call ProposeUpdates once per tick, not once per system
        combinedController.ProposeUpdatesCalls.Should().Be(1);
    }
}
