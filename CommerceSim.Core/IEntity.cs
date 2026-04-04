namespace CommerceSim.Core;

public interface IEntity
{
    /// <summary>
    /// ID that is unique across the simulation
    /// </summary>
    int Id { get; }
}
