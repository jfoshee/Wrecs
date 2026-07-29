namespace Wrecs.Core;

public interface ISystemUpdateProposer : ISystem
{
    /// <summary>
    /// Proposes a set of updates that may involve multiple Systems.
    /// Each UpdateSet represents a group of updates that should be applied atomically.
    /// </summary>
    IEnumerable<UpdateSet> ProposeUpdates();
}
