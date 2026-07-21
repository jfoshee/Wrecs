namespace Wrecs.Core;

/// <summary>
/// Enforces constraints by evaluating whether an <see cref="UpdateSet"/> is valid.
/// </summary>
public interface ISystemConstraint : ISystem
{
    ConstraintResult Validate(UpdateSet candidate);
}

public readonly record struct ConstraintResult(bool IsValid,
                                               IReadOnlyList<IEvent> Events)
{
    public static ConstraintResult Accept() => new(true, []);

    public static ConstraintResult Reject(params IEvent[] events) =>
        new(false, events);
}
