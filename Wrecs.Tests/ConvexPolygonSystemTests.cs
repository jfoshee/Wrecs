using Wrecs.Geometry;
using Wrecs.Systems;

namespace Wrecs.Tests;

public class ConvexPolygonSystemTests
{
    [Fact(DisplayName = "Unrelated entities are not registered with convex polygon system")]
    public void UnrelatedEntitiesAreNotRegistered()
    {
        var system = new ConvexPolygonSystem();
        var sim = new Sim();
        var entity = new Entity("Entity");
        sim.AddSystem(system);

        sim.InitEntities((entity, []));

        system.GetEntities().Should().BeEmpty();
    }

    [Fact(DisplayName = "Convex polygon marker entities require an initial polygon snapshot")]
    public void MarkerEntityWithoutInitialPolygonThrows()
    {
        var system = new ConvexPolygonSystem();
        var sim = new Sim();
        var entity = Mock.Of<IConvexPolygonEntity>();
        sim.AddSystem(system);

        Action act = () => sim.InitEntities((entity, []));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact(DisplayName = "Initial snapshot sets an entity convex polygon")]
    public void InitialSnapshotSetsPolygon()
    {
        var polygon = CreateSquare(new Vector2(10, 10), 4);
        var system = new ConvexPolygonSystem();
        var sim = new Sim();
        var entity = new Entity("Entity");
        sim.AddSystem(system);

        sim.InitEntities((entity, [new ConvexPolygonSnapshot(polygon)]));

        system.GetEntities().Should().Contain(entity);
        GetVertices(system.GetTypedState(entity).Polygon)
            .Should()
            .Equal(GetVertices(polygon));
    }

    [Fact(DisplayName = "Applying a convex polygon update replaces entity polygon")]
    public void ApplyUpdatesReplacesPolygon()
    {
        var system = new ConvexPolygonSystem();
        var entity = new Entity("Entity");
        var start = CreateSquare(new Vector2(0, 0), 2);
        var next = CreateSquare(new Vector2(-3, 7), 6);
        system.InitEntities((entity, new ConvexPolygonSnapshot(start)));

        system.ApplyUpdates([new ConvexPolygonUpdate(entity, next)]);

        GetVertices(system.GetTypedState(entity).Polygon)
            .Should()
            .Equal(GetVertices(next));
    }

    [Fact(DisplayName = "Required convex polygon snapshot is provided to agent")]
    public void RequiredSnapshotIsProvidedToAgent()
    {
        var polygon = CreateSquare(new Vector2(3, -1), 8);
        var agent = new ConvexPolygonAgent();
        var system = new ConvexPolygonSystem();
        var sim = new Sim();
        sim.AddSystem(system);
        sim.InitEntities((agent, [new ConvexPolygonSnapshot(polygon)]));

        sim.Tick();

        agent.ObservedPolygon.Should().NotBeNull();
        GetVertices(agent.ObservedPolygon!).Should().Equal(GetVertices(polygon));
    }

    [Fact(DisplayName = "Convex polygon system translates move intent by translating all vertices")]
    public void TranslateIntentMovesPolygon()
    {
        var polygon = CreateSquare(new Vector2(1, 2), 4);
        var agent = new ConvexPolygonAgent();
        var system = new ConvexPolygonSystem();
        system.InitEntities((agent, polygon));

        var update = system.TranslateIntent(agent, new Move2DAction(new Vector2(5, -3)))
            .Updates.OfType<ConvexPolygonUpdate>().Single();

        var expected = CreateSquare(new Vector2(6, -1), 4);
        GetVertices(update.State.Polygon).Should().Equal(GetVertices(expected));
    }

    private static ConvexPolygon CreateSquare(Vector2 bottomLeft, float size)
    {
        return new ConvexPolygon(
            [
                bottomLeft,
                bottomLeft + new Vector2(size, 0),
                bottomLeft + new Vector2(size, size),
                bottomLeft + new Vector2(0, size)
            ]);
    }

    private static Vector2[] GetVertices(ConvexPolygon polygon) => [.. polygon.Vertices];

    private sealed class ConvexPolygonAgent :
        IAgent,
        IConvexPolygonEntity,
        IAgentRequireSnapshot<ConvexPolygonSnapshot>
    {
        public int Id { get; } = EntityId.Next();
        public string Name => nameof(ConvexPolygonAgent);
        public ConvexPolygon? ObservedPolygon { get; private set; }

        public AgentIntent GetIntent(IAgentContext context)
        {
            ObservedPolygon = context.GetSnapshot<ConvexPolygonSnapshot>().Polygon;
            return AgentIntent.Empty;
        }
    }
}