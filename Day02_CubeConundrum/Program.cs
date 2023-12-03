using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

var cookie = args.FirstOrDefault("");

var client = new HttpClient
{
    BaseAddress = new("https://adventofcode.com"),
    DefaultRequestHeaders = {
        { "Cookie", cookie }
    },
};

var input = await client.GetStringAsync("2023/day/2/input");

var games = input
    .Split(Environment.NewLine)
    .Where(line => string.IsNullOrEmpty(line) is false)
    .Select(Game.Create)
    .ToList();

var result1 = games
    .Where(game => game.Subsets.All(s => s is { RedCubes: <= 12, GreenCubes: <= 13, BlueCubes: <= 14}))
    .Sum(game => game.Id);

Console.WriteLine(result1);

var result2 = games
    .Sum(game => game.Subsets.Max(s => s.RedCubes) * game.Subsets.Max(s => s.GreenCubes) * game.Subsets.Max(s => s.BlueCubes));

Console.WriteLine(result2);

internal partial record Game(int Id, ICollection<Subset> Subsets)
{
    public static Game Create(string gameLine)
    {
        var split = gameLine.Split(':');
        var id = split.First().Replace("Game ", "");
        var subsets = split.Last().Split(';').Select(Subset.Create).ToList();

        return new (int.Parse(id), subsets);        
    }
}

internal record Subset(int RedCubes, int GreenCubes, int BlueCubes)
{
    public static Subset Create(string subsetLine)
    {
        var subset = subsetLine.Split(',')
            .Select(line => line.Trim().Split(' '))
            .Select(cubes => (Count: int.Parse(cubes.First()), Colour: cubes.Last()))
            .SelectMany(cubes => Enumerable.Range(0, cubes.Count).Select(_ => cubes.Colour))
            .ToList();

        return new(
            RedCubes: subset.Count(cube => cube is "red"),
            GreenCubes: subset.Count(cube => cube is "green"),
            BlueCubes: subset.Count(cube => cube is "blue")
        );
    }
}
