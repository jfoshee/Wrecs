namespace Wrecs.Core;

public interface IIntentAction { }

public record Intent(IEnumerable<IIntentAction> Actions)
{
    public static Intent Empty { get; } = new Intent([]);

    public Intent(params IIntentAction[] actions) : this((IEnumerable<IIntentAction>)actions) { }
}
