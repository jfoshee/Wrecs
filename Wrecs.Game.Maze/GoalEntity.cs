using Wrecs.Core;
using Wrecs.Systems;

namespace Wrecs.Game.Maze;

class GoalEntity : IAlignedRectangleEntity
{
    public int Id { get; } = EntityId.Next();
    public string Name => "Goal";
}
