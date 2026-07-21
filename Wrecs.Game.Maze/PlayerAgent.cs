using System.Numerics;
using SDL3;
using Wrecs.Core;
using Wrecs.Systems;

namespace Wrecs.Game.Maze;

class PlayerAgent : ISpatial2DAgent
{
    public int Id { get; } = EntityId.Next();
    public string Name => "Player";
    private Vector2 _step = new(0, 0);
    const float speed = 2f;

    public AgentIntent GetIntent(IAgentContext context)
    {
        return new AgentIntent(new Move2DAction(_step));
    }

    public void HandleKeyboard()
    {
        var pressed = SDL.GetKeyboardState(out var _);

        var left = pressed[(int)SDL.Scancode.Left] && !pressed[(int)SDL.Scancode.Right];
        var right = pressed[(int)SDL.Scancode.Right] && !pressed[(int)SDL.Scancode.Left];
        var up = pressed[(int)SDL.Scancode.Up] && !pressed[(int)SDL.Scancode.Down];
        var down = pressed[(int)SDL.Scancode.Down] && !pressed[(int)SDL.Scancode.Up];

        var horizontalSpeed = left ? -speed : right ? speed : 0f;
        var verticalSpeed = up ? -speed : down ? speed : 0f;

        _step = new Vector2(horizontalSpeed, verticalSpeed);
    }
}
