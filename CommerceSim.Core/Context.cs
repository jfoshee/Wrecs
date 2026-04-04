namespace CommerceSim.Core;

public record class Context(CommerceSystem Commerce, Spatial.SpatialSystem Spatial, IEnumerable<IEntity> Entities);