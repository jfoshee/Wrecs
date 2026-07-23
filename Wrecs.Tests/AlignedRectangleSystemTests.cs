using Wrecs.Geometry;
using Wrecs.Systems;

namespace Wrecs.Tests;

public class AlignedRectangleSystemTests
{
    [Fact(DisplayName = "Unrelated entities are not registered")]
    public void UnrelatedEntitiesAreNotRegistered()
    {
        var system = new AlignedRectangleSystem();
        var sim = new Sim();
        var entity = new Entity("Entity");
        sim.AddSystem(system);

        sim.InitEntities((entity, []));

        // The entity does not have initial state, nor implement a marker interface, so it should not be registered with the system
        system.GetEntities().Should().BeEmpty();
    }

    [Fact(DisplayName = "Entity implementing marker interface is registered")]
    public void EntityImplementingMarkerInterfaceIsRegistered()
    {
        var system = new AlignedRectangleSystem();
        var sim = new Sim();
        var entity = Mock.Of<IAlignedRectangleEntity>();
        sim.AddSystem(system);

        sim.InitEntities((entity, []));

        system.GetEntities().Should().Contain(entity);
        system.GetTypedState(entity).Rectangle.Should().Be(AlignedRectangle.Empty);
    }

    [Fact(DisplayName = "Entity with initial state is registered and has correct aligned rectangle")]
    public void InitialSnapshotSetsAnEntityAlignedRectangle()
    {
        var system = new AlignedRectangleSystem();
        var sim = new Sim();
        var entity = new Entity("Entity");
        var rectangle = new AlignedRectangle(new Vector2(2, 3), 4, 5);
        sim.AddSystem(system);

        sim.InitEntities((entity, [new AlignedRectangleSnapshot(rectangle)]));

        system.GetEntities().Should().Contain(entity);
        system.GetTypedState(entity).Rectangle.Should().Be(rectangle);
    }

    [Fact(DisplayName = "Apply updates replaces an entity's aligned rectangle")]
    public void ApplyUpdatesReplacesAnEntityAlignedRectangle()
    {
        var system = new AlignedRectangleSystem();
        var entity = new Entity("Entity");
        var rectangle = new AlignedRectangle(new Vector2(-2, 7), 8, 3);
        system.InitEntities((entity, null));

        system.ApplyUpdates([new AlignedRectangleUpdate(entity, rectangle)]);

        system.GetTypedState(entity).Rectangle.Should().Be(rectangle);
    }

    [Fact(DisplayName = "Required aligned rectangle snapshot is provided to an agent")]
    public void RequiredAlignedRectangleSnapshotIsProvidedToAgent()
    {
        var rectangle = new AlignedRectangle(new Vector2(1, 4), 2, 6);
        var agent = new RectangleAgent();
        var system = new AlignedRectangleSystem();
        var sim = new Sim();
        sim.AddSystem(system);
        sim.InitEntities((agent, [new AlignedRectangleSnapshot(rectangle)]));

        sim.Tick();

        agent.ObservedRectangle.Should().Be(rectangle);
    }

    private sealed class RectangleAgent :
        IAgent,
        IAgentRequireSnapshot<AlignedRectangleSnapshot>
    {
        public int Id { get; } = EntityId.Next();
        public string Name => nameof(RectangleAgent);
        public AlignedRectangle? ObservedRectangle { get; private set; }

        public AgentIntent GetIntent(IAgentContext context)
        {
            ObservedRectangle = context.GetSnapshot<AlignedRectangleSnapshot>().Rectangle;
            return AgentIntent.Empty;
        }
    }
}
