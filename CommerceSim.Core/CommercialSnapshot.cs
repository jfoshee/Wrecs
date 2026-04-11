using System.Diagnostics;

namespace CommerceSim.Core;

[DebuggerDisplay("Money: {MoneyBalance}, Resources: {ResourceBalance}")]
public record struct CommercialSnapshot(int MoneyBalance, int ResourceBalance)
{
    internal CommercialSnapshot(CommercialSystem.CommercialState state) : this(state.MoneyBalance, state.ResourceBalance) { }
}
