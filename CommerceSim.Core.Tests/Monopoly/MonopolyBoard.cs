namespace CommerceSim.Core.Tests.Monopoly;

/// <summary>
/// Standard Monopoly board configuration. Array index = board position.
/// 28 purchasable properties (22 streets + 4 railroads + 2 utilities), 12 nulls.
/// </summary>
public static class MonopolyBoard
{
    public static readonly MonopolyProperty?[] Properties =
    [
        null,                                           // 0: GO
        new("Mediterranean Avenue", 60),                // 1
        null,                                           // 2: Community Chest
        new("Baltic Avenue", 60),                       // 3
        null,                                           // 4: Income Tax
        new("Reading Railroad", 200),                   // 5
        new("Oriental Avenue", 100),                    // 6
        null,                                           // 7: Chance
        new("Vermont Avenue", 100),                     // 8
        new("Connecticut Avenue", 120),                 // 9
        null,                                           // 10: Jail
        new("St. Charles Place", 140),                  // 11
        new("Electric Company", 150),                   // 12
        new("States Avenue", 140),                      // 13
        new("Virginia Avenue", 160),                    // 14
        new("Pennsylvania Railroad", 200),              // 15
        new("St. James Place", 180),                    // 16
        null,                                           // 17: Community Chest
        new("Tennessee Avenue", 180),                   // 18
        new("New York Avenue", 200),                    // 19
        null,                                           // 20: Free Parking
        new("Kentucky Avenue", 220),                    // 21
        null,                                           // 22: Chance
        new("Indiana Avenue", 220),                     // 23
        new("Illinois Avenue", 240),                    // 24
        new("B&O Railroad", 200),                       // 25
        new("Atlantic Avenue", 260),                    // 26
        new("Ventnor Avenue", 260),                     // 27
        new("Water Works", 150),                        // 28
        new("Marvin Gardens", 280),                     // 29
        null,                                           // 30: Go To Jail
        new("Pacific Avenue", 300),                     // 31
        new("North Carolina Avenue", 300),              // 32
        null,                                           // 33: Community Chest
        new("Pennsylvania Avenue", 320),                // 34
        new("Short Line", 200),                         // 35 (Railroad)
        null,                                           // 36: Chance
        new("Park Place", 350),                         // 37
        null,                                           // 38: Luxury Tax
        new("Boardwalk", 400),                          // 39
    ];

    public static MonopolyProperty? GetPropertyAtPosition(int position)
    {
        if (position < 0 || position >= Properties.Length)
            throw new IndexOutOfRangeException($"Position {position} is out of bounds for Monopoly board.");
        return Properties[position];
    }
}
