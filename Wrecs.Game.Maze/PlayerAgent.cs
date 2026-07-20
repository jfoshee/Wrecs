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
    private float verticalSpeed = 0f;
    private float horizontalSpeed = 0f;

    public AgentIntent GetIntent(IAgentContext context)
    {
        return new AgentIntent(new Move2DAction(_step));
    }

    public void HandleKey(SDL.Keycode keycode, SDL.Keymod mod, bool isPressed)
    {
        var speed = 0.02f;
        switch (keycode)
        {
            case SDL.Keycode.Up:
            case SDL.Keycode.Space:
            case SDL.Keycode.W:
                verticalSpeed = isPressed ? -speed : 0f;
                break;
            case SDL.Keycode.Down:
            case SDL.Keycode.S:
                verticalSpeed = isPressed ? speed : 0f;
                break;
            case SDL.Keycode.Left:
            case SDL.Keycode.A:
                horizontalSpeed = isPressed ? -speed : 0f;
                break;
            case SDL.Keycode.Right:
            case SDL.Keycode.D:
                horizontalSpeed = isPressed ? speed : 0f;
                break;
        }
        _step = new Vector2(horizontalSpeed, verticalSpeed);
    }
}
