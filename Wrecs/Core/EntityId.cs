namespace Wrecs.Core;

public static class EntityId
{
    private static int _counter;

    public static int Next() => Interlocked.Increment(ref _counter);
}
