namespace Wrecs.Core;

public interface ISystemEventHandler : ISystem
{
    void Handle(IEvent e);
}

public interface ISystemEventHandler<T> : ISystemEventHandler where T : IEvent
{
    void HandleTyped(T e);

    // HACK: invoke typed handler if it matches
    void ISystemEventHandler.Handle(IEvent e)
    {
        if (e is T typedEvent)
            HandleTyped(typedEvent);
    }
}
