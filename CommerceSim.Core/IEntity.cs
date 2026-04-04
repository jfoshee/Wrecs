namespace CommerceSim.Core;

public interface IEntity
{
    /// <summary>
    /// ID that is unique across the simulation
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Returns a name for this Entity.
    /// </summary>
    /// <remarks>
    /// Not necessarily unique, but should be human readable for debugging.
    /// </remarks>
    public string Name { get; }
}
