namespace CommerceSim.Core;

/// <summary>
/// Creates debit flows that remove money or resources from entities.
/// </summary>
public interface ISink : IFlowOrigin
{
    // TODO: Can/should we enforce that only creates Debits
}
