namespace Wrecs.Core;

/// <summary>
/// Creates credit flows that add money to entities.
/// </summary>
public interface IMoneySource : IMoneyFlowOrigin
{
    // TODO: Can/should we enforce that only creates Credits
}

/// <summary>
/// Creates credit flows that add resources to entities.
/// </summary>
public interface IResourceSource : IResourceFlowOrigin
{
    // TODO: Can/should we enforce that only creates Credits
}
