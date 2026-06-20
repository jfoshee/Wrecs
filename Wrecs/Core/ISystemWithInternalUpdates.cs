namespace Wrecs.Core;

/// <summary>
/// A System that prepares and applies internal updates to its own state during the Tick Update Phases.
/// </summary>
public interface ISystemWithInternalUpdates :
    ISystemInternalUpdatePreparer,
    ISystemInternalUpdateApplier;
