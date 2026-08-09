using Wrecs.Systems;

namespace Wrecs.Tests;

public class SimInitTest
{
    class BasicEntity : IEntity
    {
        public string Name => GetType().Name;
        public int Id { get; } = EntityId.Next();
    }

    class InheritsSpatial1DEntity : BasicEntity, ISpatial1DEntity
    {
    }

    class MoveAllController : ISystemUpdateProposer, IRequire<Spatial1DSystem>
    {
        private Spatial1DSystem? _spatial1dSystem;
        public void Inject(Spatial1DSystem system) => _spatial1dSystem = system;

        public IEnumerable<UpdateSet> ProposeUpdates()
        {
            var updates = _spatial1dSystem!.GetEntities()
                .Select(e => (IEntityUpdate)new Spatial1DUpdate(e, _spatial1dSystem.GetTypedState(e).Position + 1));
            yield return new(updates);
        }
    }

    class InheritsCommercialEntity : BasicEntity, ICommercialEntity
    {
    }

    interface IMarkerOnlyEntity : IEntity;

    class MarkerOnlyEntity : BasicEntity, IMarkerOnlyEntity;

    class MarkerOnlySystem : ISystemEntityStateInitializer, ISystemWithEntityMarker<IMarkerOnlyEntity>
    {
        private IReadOnlyList<IEntity> _entities = [];

        public void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState) =>
            _entities = [.. entitiesWithState.Select(e => e.entity)];

        public IReadOnlyList<IEntity> GetEntities() => _entities;
    }

    record struct StateOnlySnapshot(int Value) : IStateSnapshot<StateOnlySystem>;

    class StateOnlySystem :
        ISystemEntityStateInitializer,
        ISystemWithEntityStateSnapshots<StateOnlySnapshot>
    {
        private readonly Dictionary<IEntity, StateOnlySnapshot> _states = [];

        public void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState)
        {
            _states.Clear();
            foreach (var (entity, initialStates) in entitiesWithState)
                _states[entity] = initialStates.OfType<StateOnlySnapshot>().Single();
        }

        public IReadOnlyList<IEntity> GetEntities() => [.. _states.Keys];

        public StateOnlySnapshot GetTypedState(IEntity entity) => _states[entity];
    }

    class DynamicallyAddedAgent : BasicEntity, IAgent
    {
        public int IntentCount { get; private set; }

        public AgentIntent GetIntent(IAgentContext context)
        {
            IntentCount++;
            return AgentIntent.Empty;
        }
    }

    [Fact(DisplayName = "Systems can select entities by only a marker or only an initial state")]
    public void InitializingEntitiesForIndependentSystemConcerns()
    {
        var markerSystem = new MarkerOnlySystem();
        var stateSystem = new StateOnlySystem();
        var sim = new Sim();
        sim.AddSystems(markerSystem, stateSystem);
        var markedEntity = new MarkerOnlyEntity();
        var hasInitialStateEntity = new BasicEntity();
        var unrelatedEntity = new BasicEntity();

        sim.InitEntities(
            (markedEntity, []),
            (hasInitialStateEntity, [new StateOnlySnapshot(42)]),
            (unrelatedEntity, [])
        );

        markerSystem.GetEntities().Should().Equal(markedEntity);
        stateSystem.GetEntities().Should().Equal(hasInitialStateEntity);
        stateSystem.GetTypedState(hasInitialStateEntity).Should().Be(new StateOnlySnapshot(42));
    }

    [Fact(DisplayName = "AddEntity adds matching entities to systems that support dynamic entities")]
    public void AddingEntitiesToMatchingDynamicSystems()
    {
        var spatial1dSystem = new Spatial1DSystem();
        var sim = new Sim();
        sim.AddSystem(spatial1dSystem);
        var markedEntity = new InheritsSpatial1DEntity();
        var hasInitialStateEntity = new BasicEntity();
        var unrelatedEntity = new BasicEntity();

        sim.AddEntity(markedEntity);
        sim.AddEntity(hasInitialStateEntity, new Spatial1DSnapshot(5));
        sim.AddEntity(unrelatedEntity);

        spatial1dSystem.GetEntities().Should().Equal(markedEntity, hasInitialStateEntity);
        spatial1dSystem.GetTypedState(markedEntity).Should().Be(new Spatial1DSnapshot(0));
        spatial1dSystem.GetTypedState(hasInitialStateEntity).Should().Be(new Spatial1DSnapshot(5));
    }

    [Fact(DisplayName = "AddEntity skips matching systems that do not support dynamic entities")]
    public void AddingEntitiesSkipsInitializationOnlySystems()
    {
        var markerSystem = new MarkerOnlySystem();
        var sim = new Sim();
        sim.AddSystem(markerSystem);
        var initialEntity = new MarkerOnlyEntity();
        var addedEntity = new MarkerOnlyEntity();
        sim.InitEntities((initialEntity, []));

        sim.AddEntity(addedEntity);

        markerSystem.GetEntities().Should().Equal(initialEntity);
    }

    [Fact(DisplayName = "AddEntity adapts aggregate initial state for supporting systems")]
    public void AddingCommercialEntityWithAggregateInitialState()
    {
        var sim = new CommercialSim();
        var entity = new BasicEntity();

        sim.AddEntity(entity, new CommercialSnapshot(100, 50));

        sim.GetCommercialState(entity).Should().Be(new CommercialSnapshot(100, 50));
    }

    [Fact(DisplayName = "Dynamically added agents participate in the next tick")]
    public void AddingAgentToSimulation()
    {
        var sim = new Sim();
        var agent = new DynamicallyAddedAgent();
        sim.AddEntity(agent);

        sim.Tick();

        agent.IntentCount.Should().Be(1);
    }

    [Fact(DisplayName = "Entities inheriting ICommercialEntity or initial state are added to commercial system")]
    public void InitializingCommercialEntities()
    {
        var sim = new CommercialSim();
        var inheritsCommercialEntity = new InheritsCommercialEntity();
        var hasInitialStateEntity = new BasicEntity();
        var nonCommercialEntity = new BasicEntity();

        sim.InitEntities(
            (inheritsCommercialEntity, []),
            (nonCommercialEntity, []),
            (hasInitialStateEntity, [new CommercialSnapshot(100, 50)])
        );

        sim.GetCommercialState(inheritsCommercialEntity).Should().Be(new CommercialSnapshot(0, 0));
        sim.GetCommercialState(hasInitialStateEntity).Should().Be(new CommercialSnapshot(100, 50));
        sim.Invoking((s) => s.GetCommercialState(nonCommercialEntity)).Should().Throw<Exception>();
    }

    [Fact(DisplayName = "Entities inheriting ISpatial1DEntity or initial position are added to spatial1d system")]
    public void InitializingSpatial1DEntities()
    {
        var sim = new Sim();
        sim.AddSystem(new Spatial1DSystem());
        var inheritsSpatial1DEntity = new InheritsSpatial1DEntity();
        var hasInitialPositionEntity = new BasicEntity();
        var nonSpatial1DEntity = new BasicEntity();

        sim.InitEntities(
            (inheritsSpatial1DEntity, []),
            (nonSpatial1DEntity, []),
            (hasInitialPositionEntity, [new Spatial1DSnapshot(5)])
        );

        sim.GetSystem<Spatial1DSystem>().GetEntities().Should().HaveCount(2);
        sim.GetPosition(inheritsSpatial1DEntity).Should().Be(0);
        sim.GetPosition(hasInitialPositionEntity).Should().Be(5);
        sim.Invoking((s) => s.GetPosition(nonSpatial1DEntity)).Should().Throw<Exception>();
    }

    [Fact(DisplayName = "Spatial1D Controllers move spatial1d entities")]
    public void ControllersMoveEntities()
    {
        var sim = new Sim();
        sim.AddSystem(new Spatial1DSystem());
        var inheritsSpatial1DEntity = new InheritsSpatial1DEntity();
        var hasInitialPositionEntity = new BasicEntity();
        var nonSpatial1DEntity = new BasicEntity();
        sim.InitEntities(
            (inheritsSpatial1DEntity, []),
            (nonSpatial1DEntity, []),
            (hasInitialPositionEntity, [new Spatial1DSnapshot(5)])
        );
        sim.AddSystem(new MoveAllController());

        sim.Tick();

        sim.GetPosition(inheritsSpatial1DEntity).Should().Be(1);
        sim.GetPosition(hasInitialPositionEntity).Should().Be(6);
        sim.Invoking((s) => s.GetPosition(nonSpatial1DEntity)).Should().Throw<Exception>();
    }
}
