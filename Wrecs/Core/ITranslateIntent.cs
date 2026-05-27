namespace Wrecs.Core;

/// <summary>
/// Non-generic base that lets the Sim dispatch <see cref="IIntentAction"/>
/// instances to the right translator without knowing the concrete action type.
/// </summary>
public interface ITranslateIntent : ISystem
{
    bool CanTranslate(IIntentAction action);
    UpdateSet Translate(IAgent agent, IIntentAction action);
}

/// <summary>
/// A system that knows how to translate a specific <see cref="IIntentAction"/>
/// into a set of cross-system updates. Implementing this allows the Sim to
/// centralize agent invocation and dispatch individual actions to whichever
/// systems understand them.
/// </summary>
public interface ITranslateIntent<TAction> : ITranslateIntent where TAction : IIntentAction
{
    UpdateSet TranslateIntent(IAgent agent, TAction action);

    bool ITranslateIntent.CanTranslate(IIntentAction action) => action is TAction;
    UpdateSet ITranslateIntent.Translate(IAgent agent, IIntentAction action)
        => TranslateIntent(agent, (TAction)action);
}
