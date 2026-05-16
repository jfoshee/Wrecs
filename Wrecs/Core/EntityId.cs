namespace Wrecs.Core;

public static class EntityId
{
    private static int _counter = 0;

    public static int Next() => _counter++;
}
