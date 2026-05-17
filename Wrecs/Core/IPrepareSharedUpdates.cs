namespace Wrecs.Core;

public interface IPrepareSharedUpdates
{
    /// <summary>
    /// Prepares a set of updates that may involve multiple Systems.
    /// Each UpdateSet represents a group of updates that should be applied atomically.
    /// </summary>
    IEnumerable<UpdateSet> PrepareSharedUpdates();
}
