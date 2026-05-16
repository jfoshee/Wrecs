namespace Wrecs.Tests;

public record TestEntity(int Id, string Name = "Test") : IEntity;
public record struct StateA(int Value) : IStateSnapshot<SystemA>;
public record struct StateB(string Data) : IStateSnapshot<SystemB>;

public class SystemA : IControllableSystem<TestEntity, StateA>
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
    public StateA GetState(IEntity entity) => _states.TryGetValue(entity, out var s) ? s : default;
    public void ApplyUpdates(IEnumerable<EntityUpdate<StateA>> updates)
    {
        foreach (var update in updates)
            _states[update.Entity] = update.State;
    }

    public void PrepareInternalUpdates() { }
    public void ApplyInternalUpdates() { }
}

public class SystemB : IControllableSystem<TestEntity, StateB>
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
    public StateB GetState(IEntity entity) => _states.TryGetValue(entity, out var s) ? s : default;
    public void ApplyUpdates(IEnumerable<EntityUpdate<StateB>> updates)
    {
        foreach (var update in updates)
            _states[update.Entity] = update.State;
    }

    public void PrepareInternalUpdates() { }
    public void ApplyInternalUpdates() { }
}

public class ControllerA : IController<StateA>
{
    public int GetEntitiesCalls { get; private set; }
    public IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> allEntities)
    {
        GetEntitiesCalls++;
        return allEntities;
    }

    public StateA GetNewState(IEntity entity, StateA currentState) => new(currentState.Value + 1);
}

public class ControllerB : IController<StateB>
{
    public int GetEntitiesCalls { get; private set; }
    public IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> allEntities)
    {
        GetEntitiesCalls++;
        return allEntities;
    }

    public StateB GetNewState(IEntity entity, StateB currentState) => new(currentState.Data + "B");
}

public class CombinedController : IController<StateA>, IController<StateB>
{
    public int GetEntitiesCalls { get; private set; }
    public IEnumerable<IEntity> GetEntitiesToUpdate(IEnumerable<IEntity> allEntities)
    {
        GetEntitiesCalls++;
        return allEntities;
    }

    public StateA GetNewState(IEntity entity, StateA currentState) => new(currentState.Value + 10);
    public StateB GetNewState(IEntity entity, StateB currentState) => new(currentState.Data + "C");
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
        sim.InitControllers(controllerA, controllerB);

        sim.Tick();

        systemA.GetState(entity).Value.Should().Be(11);
        systemB.GetState(entity).Data.Should().Be("TestB");

        controllerA.GetEntitiesCalls.Should().Be(1);
        controllerB.GetEntitiesCalls.Should().Be(1);
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
        sim.InitControllers(combinedController);

        sim.Tick();

        // Verify state changed in both systems
        systemA.GetState(entity).Value.Should().Be(20);
        systemB.GetState(entity).Data.Should().Be("TestC");

        // Key behavior: Should only query entities once per tick, not once per system
        combinedController.GetEntitiesCalls.Should().Be(1);
    }
}
