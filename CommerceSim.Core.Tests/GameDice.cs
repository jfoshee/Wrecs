namespace CommerceSim.Core.Tests;

public interface IGameDice
{
    int Roll();
}

public class GameDice : IGameDice
{
    private readonly Random _random = new();

    /// <summary>
    /// Rolls a six-sided die and returns a value between 1 and 6.
    /// </summary>
    public int Roll() => _random.Next(1, 7);
}
