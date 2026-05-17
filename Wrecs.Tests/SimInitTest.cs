using Wrecs.Systems;

namespace Wrecs.Tests;

public class SimInitTest
{
    class BasicEntity : IEntity
    {
        public string Name => GetType().Name;
        public int Id { get; } = EntityId.Next();
    }

    class InheritsSpatialEntity : BasicEntity, ISpatialEntity
    {
    }

    class MoveAllController : ISpatialController, IRequire<SpatialSystem>
    {
        private SpatialSystem? _spatialSystem;
        public void Inject(SpatialSystem system) => _spatialSystem = system;

        public IEnumerable<UpdateSet> PrepareSharedUpdates()
        {
            var updates = _spatialSystem!.GetEntities()
                .Select(e => (IEntityUpdate)new EntityUpdate<PositionSnapshot>(e, new(_spatialSystem.GetState(e).Position + 1)));
            yield return new(updates);
        }
    }

    class InheritsCommercialEntity : BasicEntity, ICommercialEntity
    {
    }

    [Fact(DisplayName = "Entities inheriting ICommercialEntity or initial state are added to commercial system")]
    public void InitializingCommercialEntities()
    {
        var sim = new Sim();
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

    [Fact(DisplayName = "Entities inheriting ISpatialEntity or initial position are added to spatial system")]
    public void InitializingSpatialEntities()
    {
        var sim = new Sim();
        var inheritsSpatialEntity = new InheritsSpatialEntity();
        var hasInitialPositionEntity = new BasicEntity();
        var nonSpatialEntity = new BasicEntity();

        sim.InitEntities(
            (inheritsSpatialEntity, []),
            (nonSpatialEntity, []),
            (hasInitialPositionEntity, [new PositionSnapshot(5)])
        );

        sim.GetPosition(inheritsSpatialEntity).Should().Be(0);
        sim.GetPosition(hasInitialPositionEntity).Should().Be(5);
        sim.Invoking((s) => s.GetPosition(nonSpatialEntity)).Should().Throw<Exception>();
    }

    [Fact(DisplayName = "Spatial Controllers move spatial entities")]
    public void ControllersMoveEntities()
    {
        var sim = new Sim();
        var inheritsSpatialEntity = new InheritsSpatialEntity();
        var hasInitialPositionEntity = new BasicEntity();
        var nonSpatialEntity = new BasicEntity();
        sim.InitEntities(
            (inheritsSpatialEntity, []),
            (nonSpatialEntity, []),
            (hasInitialPositionEntity, [new PositionSnapshot(5)])
        );
        sim.InitControllers(new MoveAllController());

        sim.Tick();

        sim.GetPosition(inheritsSpatialEntity).Should().Be(1);
        sim.GetPosition(hasInitialPositionEntity).Should().Be(6);
        sim.Invoking((s) => s.GetPosition(nonSpatialEntity)).Should().Throw<Exception>();
    }
}
