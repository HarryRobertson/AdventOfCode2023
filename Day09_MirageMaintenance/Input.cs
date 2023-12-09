using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace AdventOfCode;

internal static class Input
{
    public static async Task<string> TestAsync(string filename = "Test.txt")
    {
        return await File.ReadAllTextAsync(filename);
    }

    public static async Task<string> RealAsync(int day, string cookie)
    {
        var client = new HttpClient
        {
            BaseAddress = new("https://adventofcode.com"),
            DefaultRequestHeaders = {
                { "Cookie", cookie }
            },
        };
        return await client.GetStringAsync($"2023/day/{day}/input");
    }
}