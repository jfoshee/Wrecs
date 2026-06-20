namespace Wrecs.Core;

public interface IEvent;

public interface ISystemEventRaiser
{
    IEnumerable<IEvent> GetEvents();
}
public interface ISystemEventRaiser<T> : ISystemEventRaiser where T : IEvent
{
    /// <summary>
    /// Return new events to raise for this Tick.
    /// </summary>
    /// <returns></returns>
    IEnumerable<T> GetTypedEvents();

    IEnumerable<IEvent> ISystemEventRaiser.GetEvents() => GetTypedEvents().Cast<IEvent>();
}
