using Wrecs.Core;
using Wrecs.Systems;

namespace Wrecs.Game.Maze;

class ConfettiEntity(byte Red, byte Green, byte Blue, float Size) : IVelocity2DEntity
{
    public int Id { get; } = EntityId.Next();
    public string Name => $"Confetti-{Id}";
    public byte Red { get; } = Red;
    public byte Green { get; } = Green;
    public byte Blue { get; } = Blue;
    public float Size { get; } = Size;
}