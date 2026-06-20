namespace Wrecs.Core;

public interface ISystemInternalUpdatePreparer : ISystem
{
    /// <summary>
    /// Using the current state of the world, prepares any internal updates
    /// that this System will perform on its own state during this Tick.
    /// </summary>
    void PrepareInternalUpdates();
}
