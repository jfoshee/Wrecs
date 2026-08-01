using Wrecs.Geometry;
using Wrecs.Systems;

namespace Wrecs.Tests;

public class CircleSystemTests
{
    [Fact(DisplayName = "Unrelated entities are not registered with circle system")]
    public void UnrelatedEntitiesAreNotRegistered()
    {
        var system = new CircleSystem();
        var sim = new Sim();
        var entity = new Entity("Entity");
        sim.AddSystem(system);

        sim.InitEntities((entity, []));

        system.GetEntities().Should().BeEmpty();
    }

    [Fact(DisplayName = "Circle marker entity is registered with default circle")]
    public void MarkerEntityIsRegisteredWithDefaultCircle()
    {
        var system = new CircleSystem();
        var sim = new Sim();
        var entity = Mock.Of<ICircleEntity>();
        sim.AddSystem(system);

        sim.InitEntities((entity, []));

        system.GetEntities().Should().Contain(entity);
        system.GetTypedState(entity).Circle.Should().Be(default(Circle));
    }

    [Fact(DisplayName = "Initial snapshot sets an entity circle")]
    public void InitialSnapshotSetsCircle()
    {
        var circle = new Circle(new Vector2(2, 3), 4);
        var system = new CircleSystem();
        var sim = new Sim();
        var entity = new Entity("Entity");
        sim.AddSystem(system);

        sim.InitEntities((entity, [new CircleSnapshot(circle)]));

        system.GetEntities().Should().Contain(entity);
        system.GetTypedState(entity).Circle.Should().Be(circle);
    }

    [Fact(DisplayName = "Applying a circle update replaces entity circle")]
    public void ApplyUpdatesReplacesCircle()
    {
        var system = new CircleSystem();
        var entity = new Entity("Entity");
        var circle = new Circle(new Vector2(-2, 7), 3);
        system.InitEntities((entity, null));

        system.ApplyUpdates([new CircleUpdate(entity, circle)]);

        system.GetTypedState(entity).Circle.Should().Be(circle);
    }

    [Fact(DisplayName = "Required circle snapshot is provided to agent")]
    public void RequiredSnapshotIsProvidedToAgent()
    {
        var circle = new Circle(new Vector2(1, 4), 2);
        var agent = new CircleAgent();
        var system = new CircleSystem();
        var sim = new Sim();
        sim.AddSystem(system);
        sim.InitEntities((agent, [new CircleSnapshot(circle)]));

        sim.Tick();

        agent.ObservedCircle.Should().Be(circle);
    }

    [Fact(DisplayName = "Circle system translates move intent without changing radius")]
    public void TranslateIntentMovesCenter()
    {
        var circle = new Circle(new Vector2(1, 4), 2);
        var agent = new CircleAgent();
        var system = new CircleSystem();
        system.InitEntities((agent, circle));

        var update = system.TranslateIntent(agent, new Move2DAction(new Vector2(3, -5)))
            .Updates.OfType<CircleUpdate>().Single();

        update.State.Circle.Should().Be(new Circle(new Vector2(4, -1), 2));
    }

    private sealed class CircleAgent :
        IAgent,
        IAgentRequireSnapshot<CircleSnapshot>
    {
        public int Id { get; } = EntityId.Next();
        public string Name => nameof(CircleAgent);
        public Circle? ObservedCircle { get; private set; }

        public AgentIntent GetIntent(IAgentContext context)
        {
            ObservedCircle = context.GetSnapshot<CircleSnapshot>().Circle;
            return AgentIntent.Empty;
        }
    }
}
