namespace Wrecs.Core;

/// <summary>
/// Non-generic base that lets the Sim dispatch <see cref="IAgentIntentAction"/>
/// instances to the right translator without knowing the concrete action type.
/// </summary>
public interface ISystemAgentIntentTranslator : ISystem
{
    bool CanTranslate(IAgentIntentAction action);
    UpdateSet Translate(IAgent agent, IAgentIntentAction action);
}

/// <summary>
/// A system that knows how to translate a specific <see cref="IAgentIntentAction"/>
/// into a set of cross-system updates. Implementing this allows the Sim to
/// centralize agent invocation and dispatch individual actions to whichever
/// systems understand them.
/// </summary>
public interface ISystemAgentIntentTranslator<TAction> : ISystemAgentIntentTranslator where TAction : IAgentIntentAction
{
    UpdateSet TranslateIntent(IAgent agent, TAction action);

    bool ISystemAgentIntentTranslator.CanTranslate(IAgentIntentAction action) => action is TAction;
    UpdateSet ISystemAgentIntentTranslator.Translate(IAgent agent, IAgentIntentAction action)
        => TranslateIntent(agent, (TAction)action);
}
