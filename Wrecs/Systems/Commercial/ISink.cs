namespace Wrecs.Systems.Commercial;

/// <summary>
/// Creates debit flows that remove money from entities.
/// </summary>
public interface IMoneySink : IMoneyFlowOrigin
{
    // TODO: Can/should we enforce that only creates Debits
}

/// <summary>
/// Creates debit flows that remove resources from entities.
/// </summary>
public interface IResourceSink : IResourceFlowOrigin
{
    // TODO: Can/should we enforce that only creates Debits
}
