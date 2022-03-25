using System;
using System.Security.Cryptography;

namespace KBot.Modules.Gambling.Objects;

public static class Generators
{
    private static readonly RandomNumberGenerator Generator = RandomNumberGenerator.Create();
    public static string GenerateID()
    {
        var ticks = new DateTime(2016, 1, 1).Ticks;
        var ans = DateTime.Now.Ticks - ticks;
        return ans.ToString("x");
    }

    public static int RandomNumberBetween(int minimumValue, int maximumValue)
    {
        var randomNumber = new byte[1];
        Generator.GetBytes(randomNumber);
        var asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);
        var multiplier = Math.Max(0, (asciiValueOfRandomCharacter / 255d) - 0.00000000001d);
        var range = maximumValue - minimumValue + 1;
        var randomValueInRange = Math.Floor(multiplier * range);
        return (int)(minimumValue + randomValueInRange);
    }
}