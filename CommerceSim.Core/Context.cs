namespace CommerceSim.Core;

public record class Context(Sim CommerceSim, Spatial.SpatialSim SpatialSim, IEnumerable<IEntity> Entities);