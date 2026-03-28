namespace CommerceSim.Core.Agents;

internal static class AgentId
{
    private static int _counter = 0;

    public static int Next() => _counter++;
}
