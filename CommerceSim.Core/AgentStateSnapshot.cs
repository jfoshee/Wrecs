using System.Diagnostics;

namespace CommerceSim.Core;

[DebuggerDisplay("Money: {MoneyBalance}, Resources: {ResourceBalance}")]
public record struct AgentStateSnapshot(int MoneyBalance, int ResourceBalance)
{
    internal AgentStateSnapshot(Sim.AgentState state) : this(state.MoneyBalance, state.ResourceBalance) { }
}
