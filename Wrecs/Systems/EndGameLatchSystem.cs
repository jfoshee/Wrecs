using System.Runtime.CompilerServices;
using Wrecs.Core;

namespace Wrecs.Systems;

/// <summary>
/// Sets the given <paramref name="GameEnded"/> flag to true when an <see cref="EndGameEvent"/> is raised.
/// This can be used for terminating a game loop
/// </summary>
public class EndGameLatchSystem(StrongBox<bool> GameEnded) : ISystemEventHandler<EndGameEvent>
{
    public void HandleTyped(EndGameEvent e)
    {
        GameEnded.Value = true;
    }
}
