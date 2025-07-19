namespace Dina;

using System;
using System.Collections.Generic;
using System.ComponentModel;

using Microsoft.SemanticKernel;

public class TestFunctions
{
    [KernelFunction, Description("Get a random theme name for an art piece.")]
    public string RandomTheme()
    {
        var list = new List<string> { "boo", "dishes", "art", "needle", "tank", "police" };
        return list[new Random().Next(0, list.Count)];
    }

    [KernelFunction, Description("Get the current time for a city")]
    public string GetCurrentTime(string city) => $"It is {DateTime.Now.Hour}:{DateTime.Now.Minute} in {city}.";
}
