namespace CommerceSim.Core;

public record struct AgentStateSnapshot(int MoneyBalance, int ResourceBalance)
{
    internal AgentStateSnapshot(Sim.AgentState state) : this(state.MoneyBalance, state.ResourceBalance) { }
}
