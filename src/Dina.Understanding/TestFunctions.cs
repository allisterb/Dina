using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.SemanticKernel;


namespace Dina;

public class TestFunctions
{
    [KernelFunction, Description("Take the square root of a number")]
    public string RandomTheme()
    {
        var list = new List<string> { "boo", "dishes", "art", "needle", "tank", "police" };
        return list[new Random().Next(0, list.Count)];
    }

    [KernelFunction, Description("Get the current time for a city")]
    public string GetCurrentTime(string city) => $"It is {DateTime.Now.Hour}:{DateTime.Now.Minute} in {city}.";
}
