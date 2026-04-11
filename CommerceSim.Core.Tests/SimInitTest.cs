using CommerceSim.Core.Spatial;

namespace CommerceSim.Core.Tests;

public class SimInitTest
{
    class BasicEntity : IEntity
    {
        public string Name => nameof(BasicEntity);
        public int Id { get; } = EntityId.Next();
    }

    class InheritsSpatialEntity : ISpatialEntity
    {
        public string Name => nameof(InheritsSpatialEntity);
        public int Id { get; } = EntityId.Next();
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
        sim.Invoking((s) => s.GetPosition(nonSpatialEntity)).Should().Throw<KeyNotFoundException>();
    }
}
