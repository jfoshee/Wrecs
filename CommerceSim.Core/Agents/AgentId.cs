namespace CommerceSim.Core.Agents;

public static class AgentId
{
    private static int _counter = 0;

    public static int Next() => _counter++;
}
