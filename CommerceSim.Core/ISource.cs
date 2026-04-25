namespace CommerceSim.Core;

/// <summary>
/// Creates credit flows that add money or resources to entities.
/// </summary>
public interface ISource : IFlowOrigin
{
    // TODO: Can/should we enforce that only creates Credits
}
