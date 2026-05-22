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

    class MoveAllController : IPrepareSharedUpdates, IRequire<Spatial1DSystem>
    {
        private Spatial1DSystem? _spatial1dSystem;
        public void Inject(Spatial1DSystem system) => _spatial1dSystem = system;

        public IEnumerable<UpdateSet> PrepareSharedUpdates()
        {
            var updates = _spatial1dSystem!.GetEntities()
                .Select(e => (IEntityUpdate)new EntityUpdate<PositionSnapshot>(e, new(_spatial1dSystem.GetTypedState(e).Position + 1)));
            yield return new(updates);
        }
    }

    class InheritsCommercialEntity : BasicEntity, ICommercialEntity
    {
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
            (hasInitialPositionEntity, [new PositionSnapshot(5)])
        );

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
            (hasInitialPositionEntity, [new PositionSnapshot(5)])
        );
        sim.AddSystems(new MoveAllController());

        sim.Tick();

        sim.GetPosition(inheritsSpatial1DEntity).Should().Be(1);
        sim.GetPosition(hasInitialPositionEntity).Should().Be(6);
        sim.Invoking((s) => s.GetPosition(nonSpatial1DEntity)).Should().Throw<Exception>();
    }
}
