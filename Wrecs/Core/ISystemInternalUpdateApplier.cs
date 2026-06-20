namespace Wrecs.Core;

public interface ISystemInternalUpdateApplier : ISystem
{
    /// <summary>
    /// Applies any internal updates to this System's own state for this Tick.
    /// </summary>
    void ApplyInternalUpdates();
}
