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

    public AgentIntent GetIntent(IAgentContext context)
    {
        return new AgentIntent(new Move2DAction(_step));
    }

    public void HandleKey(SDL.Keycode keycode, SDL.Keymod mod, bool isPressed)
    {
        float x = 0, y = 0;
        _step = new Vector2(0, 0);
        var speed = 0.02f;
        if (isPressed)
        {
            switch (keycode)
            {
                case SDL.Keycode.Up:
                case SDL.Keycode.Space:
                case SDL.Keycode.W:
                    y = -speed;
                    break;
                case SDL.Keycode.Down:
                case SDL.Keycode.S:
                    y = speed;
                    break;
                case SDL.Keycode.Left:
                case SDL.Keycode.A:
                    x = -speed;
                    break;
                case SDL.Keycode.Right:
                case SDL.Keycode.D:
                    x = speed;
                    break;
            }
            _step = new Vector2(x, y);
        }
    }
}
