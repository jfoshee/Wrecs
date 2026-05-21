namespace Wrecs.Tests;

public class EventsTests
{
    struct BasicEvent : IEvent;
    class BasicRaiser : IRaise<BasicEvent>, ISystem
    {
        public List<BasicEvent> ToRaise = [];
        public IEnumerable<BasicEvent> GetTypedEvents()
        {
            IEnumerable<BasicEvent> r = [.. ToRaise];
            ToRaise.Clear();
            return r;
        }

        public void ApplyInternalUpdates() { }
        public void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState) { }
        public void PrepareInternalUpdates() { }
    }

    class BasicHandler : IHandle<BasicEvent>, ISystem
    {
        public int Handled { get; private set; }

        public void HandleTyped(BasicEvent e)
        {
            ++Handled;
        }

        public void ApplyInternalUpdates() { }
        public void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState) { }
        public void PrepareInternalUpdates() { }
    }

    [Fact(DisplayName = "0 Raised, 0 Handled")]
    public void Zero()
    {
        var sim = new Sim();
        var raiser = new BasicRaiser();
        var handler = new BasicHandler();
        sim.AddSystem(raiser);
        sim.AddSystem(handler);

        sim.Tick();

        handler.Handled.Should().Be(0);
    }

    [Fact(DisplayName = "1 Raised, 1 Handled")]
    public void One()
    {
        var sim = new Sim();
        var raiser = new BasicRaiser();
        var handler = new BasicHandler();
        sim.AddSystem(raiser);
        sim.AddSystem(handler);
        raiser.ToRaise.Add(new());

        sim.Tick();

        handler.Handled.Should().Be(1);
    }

    [Fact(DisplayName = "1 Raised, 1 Handled, Only once")]
    public void ClearedNextTick()
    {
        var sim = new Sim();
        var raiser = new BasicRaiser();
        var handler = new BasicHandler();
        sim.AddSystem(raiser);
        sim.AddSystem(handler);
        raiser.ToRaise.Add(new());

        sim.Tick();
        sim.Tick();

        handler.Handled.Should().Be(1);
    }

    [Fact(DisplayName = "1 Raised, 2 Handlers")]
    public void TwoHandlers()
    {
        var sim = new Sim();
        var raiser = new BasicRaiser();
        var handler1 = new BasicHandler();
        var handler2 = new BasicHandler();
        sim.AddSystem(raiser);
        sim.AddSystem(handler1);
        sim.AddSystem(handler2);
        raiser.ToRaise.Add(new());

        sim.Tick();

        handler1.Handled.Should().Be(1);
        handler2.Handled.Should().Be(1);
    }

    struct AnotherEvent : IEvent;
    class AnotherRaiser : IRaise<AnotherEvent>, ISystem
    {
        public List<AnotherEvent> ToRaise = [];
        public IEnumerable<AnotherEvent> GetTypedEvents()
        {
            IEnumerable<AnotherEvent> r = [.. ToRaise];
            ToRaise.Clear();
            return r;
        }

        public void ApplyInternalUpdates() { }
        public void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState) { }
        public void PrepareInternalUpdates() { }
    }

    class AnotherHandler : IHandle<AnotherEvent>, ISystem
    {
        public int Handled { get; private set; }

        public void HandleTyped(AnotherEvent e)
        {
            ++Handled;
        }

        public void ApplyInternalUpdates() { }
        public void InitEntities(IEnumerable<(IEntity entity, IStateSnapshot[] initialStates)> entitiesWithState) { }
        public void PrepareInternalUpdates() { }
    }

    [Fact(DisplayName = "Event Type Distinction")]
    public void TypeDistinction()
    {
        var sim = new Sim();
        var basicRaiser = new BasicRaiser(); sim.AddSystem(basicRaiser);
        var basicHandler = new BasicHandler(); sim.AddSystem(basicHandler);
        var anotherRaiser = new AnotherRaiser(); sim.AddSystem(anotherRaiser);
        var anotherHandler = new AnotherHandler(); sim.AddSystem(anotherHandler);

        for (var i = 0; i != 4; ++i)
            basicRaiser.ToRaise.Add(new());
        for (var i = 0; i != 8; ++i)
            anotherRaiser.ToRaise.Add(new());

        sim.Tick();

        basicHandler.Handled.Should().Be(4);
        anotherHandler.Handled.Should().Be(8);
    }
}
