using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AdventOfCode;

var cookie = args.FirstOrDefault("");

var input = cookie switch
{
    "test1" => await Input.TestAsync("Test1.txt"),
    _ => await Input.RealAsync(9, cookie),
};

var histories = input
    .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
    .Select(line => line
        .Split(' ', StringSplitOptions.TrimEntries)
        .Select(int.Parse)
        .ToPrintableCollection());

var result1 = histories
    .Sum(history => history.ExtrapolateForwards());
Console.WriteLine($"Result 1: {result1}");

var result2 = histories
    .Sum(history => history.ExtrapolateBackwards());
Console.WriteLine($"Result 2: {result2}");

internal static class DomainExtensions
{
    public static TNumber ExtrapolateForwards<TNumber>(this IEnumerable<TNumber> numbers) where TNumber : INumber<TNumber>, IMinMaxValue<TNumber>
    {
        return numbers.GenerateDifferences()
            .Reverse()
            .Sum(differences => differences.Last());
    }

    public static TNumber ExtrapolateBackwards<TNumber>(this IEnumerable<TNumber> numbers) where TNumber : INumber<TNumber>, IMinMaxValue<TNumber>
    {
        return numbers.GenerateDifferences()
            .Reverse()
            .Subtract(differences => differences.First());
    }

    private static IEnumerable<IEnumerable<TNumber>> GenerateDifferences<TNumber>(this IEnumerable<TNumber> numbers) where TNumber : INumber<TNumber>, IMinMaxValue<TNumber>
    {
        var lastDifferences = numbers.ToList();
        while (lastDifferences.Distinct().ToArray() is not [ 0 ])
        {
            yield return lastDifferences;
            lastDifferences = lastDifferences
                .Aggregate((differences: new List<TNumber>(), last: TNumber.Zero), 
                    (aggregator, number) => ([..aggregator.differences, number - aggregator.last], number),
                    aggregator => aggregator.differences)
                .Skip(1)
                .ToList();
        }
    }

    private static TNumber Sum<T, TNumber>(this IEnumerable<T> numbers, Func<T, TNumber> selector) 
        where TNumber : INumber<TNumber>, IMinMaxValue<TNumber>
    {
        return numbers
            .Select(selector)
            .Aggregate((lastDifference, difference) => difference + lastDifference);
    }

    private static TNumber Subtract<T, TNumber>(this IEnumerable<T> numbers, Func<T, TNumber> selector) 
        where TNumber : INumber<TNumber>, IMinMaxValue<TNumber>
    {
        return numbers
            .Select(selector)
            .Aggregate((lastDifference, difference) => difference - lastDifference);
    }
}
