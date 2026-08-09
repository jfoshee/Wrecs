using System.Numerics;
using SDL3;
using Wrecs.Core;
using Wrecs.Systems;

namespace Wrecs.Game.Polygons;

class PlayerAgent(float Speed, float SprintMultiplier) : ISpatial2DAgent, ICircleEntity
{
    public int Id { get; } = EntityId.Next();
    public string Name => "Player";

    private Vector2 _step = Vector2.Zero;

    public AgentIntent GetIntent(IAgentContext context) =>
        _step == Vector2.Zero
            ? AgentIntent.Empty
            : new(new Move2DAction(_step));

    public void HandleKeyboard()
    {
        var pressed = SDL.GetKeyboardState(out var _);

        var speed = pressed[(int)SDL.Scancode.LShift] || pressed[(int)SDL.Scancode.RShift]
            ? Speed * SprintMultiplier
            : Speed;

        var left = pressed[(int)SDL.Scancode.Left] && !pressed[(int)SDL.Scancode.Right];
        var right = pressed[(int)SDL.Scancode.Right] && !pressed[(int)SDL.Scancode.Left];
        var up = pressed[(int)SDL.Scancode.Up] && !pressed[(int)SDL.Scancode.Down];
        var down = pressed[(int)SDL.Scancode.Down] && !pressed[(int)SDL.Scancode.Up];

        var horizontalSpeed = left ? -speed : right ? speed : 0f;
        var verticalSpeed = up ? -speed : down ? speed : 0f;

        _step = new Vector2(horizontalSpeed, verticalSpeed);
    }
}
