namespace CommerceSim.Core;

public record class Context(CommerceSystem CommerceSim, Spatial.SpatialSim SpatialSim, IEnumerable<IEntity> Entities);