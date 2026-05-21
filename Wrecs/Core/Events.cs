namespace Wrecs.Core;

public interface IEvent;

public interface IRaise
{
    IEnumerable<IEvent> GetEvents();
}
public interface IRaise<T> : IRaise where T : IEvent
{
    /// <summary>
    /// Return new events to raise for this Tick.
    /// </summary>
    /// <returns></returns>
    IEnumerable<T> GetTypedEvents();

    IEnumerable<IEvent> IRaise.GetEvents() => GetTypedEvents().Cast<IEvent>();
}

public interface IHandle
{
    void Handle(IEvent e);
}

public interface IHandle<T> : IHandle where T : IEvent
{
    void HandleTyped(T e);

    // HACK: invoke typed handler if it matches
    void IHandle.Handle(IEvent e)
    {
        if (e is T typedEvent)
            HandleTyped(typedEvent);
    }
}
