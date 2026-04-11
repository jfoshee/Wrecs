using System.Runtime.CompilerServices;
using CommerceSim.Core.Spatial;
using Position = int;

namespace CommerceSim.Core.Tests;

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

    class MoveAllController : ISpatialController
    {
        public IEnumerable<IEntity> GetEntitiesToMove(IEnumerable<IEntity> entities) => entities;
        public Position GetNewPosition(IEntity _, Position currentPosition) => currentPosition + 1;
    }

    [Fact(DisplayName = "Entities inheriting ISpatialEntity or initial position are added to spatial system")]
    public void InitializingSpatialEntities()
    {
        var sim = new Sim();
        var inheritsSpatialEntity = new InheritsSpatialEntity();
        var hasInitialPositionEntity = new BasicEntity();
        var nonSpatialEntity = new BasicEntity();

        sim.InitEntities(
            (inheritsSpatialEntity, null, null),
            (nonSpatialEntity, null, null),
            (hasInitialPositionEntity, null, 5)
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
            (inheritsSpatialEntity, null, null),
            (nonSpatialEntity, null, null),
            (hasInitialPositionEntity, null, 5)
        );
        sim.InitControllers(new MoveAllController());

        sim.Tick();

        sim.GetPosition(inheritsSpatialEntity).Should().Be(1);
        sim.GetPosition(hasInitialPositionEntity).Should().Be(6);
        sim.Invoking((s) => s.GetPosition(nonSpatialEntity)).Should().Throw<Exception>();
    }
}
