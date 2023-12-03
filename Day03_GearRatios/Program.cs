using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

static async Task<string> GetRealInput(string cookie)
{
    var client = new HttpClient
    {
        BaseAddress = new("https://adventofcode.com"),
        DefaultRequestHeaders = {
            { "Cookie", cookie }
        },
    };
    return await client.GetStringAsync("2023/day/3/input");
}

static string GetTestInput()
{
    return """
    467..114..
    ...*......
    ..35..633.
    ......#...
    617*......
    .....+.58.
    ..592.....
    ......755.
    ...$.*....
    .664.598..

    """;
}

var cookie = args.FirstOrDefault("");

var input = cookie switch
{
    "test" => GetTestInput(),
    _ => await GetRealInput(cookie),
};

var grid = input
    .Split(Environment.NewLine)
    .Where(line => string.IsNullOrWhiteSpace(line) is false)
    .Select(line => line.ToCharArray()
        .Where(c => c is not '\r' or '\n')
        .Select(c => c switch
        {
            '.' => Schema.CreatePeriod(),
            char digit when char.IsDigit(digit) => Schema.CreateNumber(digit),
            char symbol => Schema.CreateSymbol(symbol),
        })
        .MergeAdjacentNumbers())
    .ToTwoDimensionalArray();

var (gridDepth, gridWidth) = (grid.GetLength(0), grid.GetLength(1));

var result1 = grid.GetSymbolCoordinates()
    .SelectSearchCoordinates()
    .SelectMany(coords => coords
        .ExceptOutOfBounds(gridDepth, gridWidth)
        .DistinctNumbersSelectedFromGrid(grid))
    .Sum(n => n.Value);
Console.WriteLine($"Result 1: {result1}");

var result2 = grid.GetGearSymbolCoordinates()
    .SelectSearchCoordinates()
    .Select(coords => coords
        .ExceptOutOfBounds(gridDepth, gridWidth)
        .DistinctNumbersSelectedFromGrid(grid)
        .ToArray())
    .Where(coords => coords is { Length: 2 })
    .Sum(ratios => ratios[0].Value * ratios[1].Value);
Console.WriteLine($"Result 2: {result2}");

internal static class Extensions
{
    private static IEnumerable<Number> FindNextNumbers(this LinkedList<Schema> schemas, Number number)
    {
        yield return number;

        var current = schemas.Find(number) ?? throw new InvalidOperationException();
        while (current.Next is { Value: Number nextValue } next)
        {
            yield return nextValue;
            current = next;
        }
    }

    public static IEnumerable<Schema> MergeAdjacentNumbers(this IEnumerable<Schema> schemas)
    {
        var linked = new LinkedList<Schema>(schemas);

        int skip = 0;
        foreach (var schema in linked)
        {
            if (skip-- > 0) continue;

            if (schema is Number number)
            {
                var numbers = linked.FindNextNumbers(number).ToList();
                var merged = Number.Merge(numbers);
                foreach (var _ in numbers)
                    yield return merged;

                skip = numbers.Count - 1;
            }
            else yield return schema;
        }
    }

    public static IEnumerable<(int rowNumber, int colNumber)> GetSymbolCoordinates(this Schema[,] grid)
    {
        return grid.AsEnumerable()
            .SelectMany((row, rowNumber) => row
                .Select((col, colNumber) => (schema: col, rowNumber, colNumber)))
            .Where(schema => schema is { schema: Symbol })
            .Select(symbol => (symbol.rowNumber, symbol.colNumber));
    }

    public static IEnumerable<(int rowNumber, int colNumber)> GetGearSymbolCoordinates(this Schema[,] grid)
    {
        return grid.AsEnumerable()
            .SelectMany((row, rowNumber) => row
                .Select((col, colNumber) => (schema: col, rowNumber, colNumber)))
            .Where(schema => schema is { schema: Symbol { Value: '*' } })
            .Select(symbol => (symbol.rowNumber, symbol.colNumber));
    }

    public static IEnumerable<IEnumerable<(int rowNumber, int colNumber)>> SelectSearchCoordinates(this IEnumerable<(int rowNumber, int colNumber)> coordinates)
    {
        return coordinates.Select(GenerateSearchCoordinates);

        static IEnumerable<(int rowNumber, int colNumber)> GenerateSearchCoordinates((int, int) coordinate)
        {
            var (rowNumber, colNumber) = coordinate;

            yield return (rowNumber - 1, colNumber - 1);
            yield return (rowNumber - 1, colNumber);
            yield return (rowNumber - 1, colNumber + 1);

            yield return (rowNumber, colNumber - 1);
            yield return (rowNumber, colNumber + 1);

            yield return (rowNumber + 1, colNumber - 1);
            yield return (rowNumber + 1, colNumber);
            yield return (rowNumber + 1, colNumber + 1);
        }
    }

    public static IEnumerable<(int rowNumber, int colNumber)> ExceptOutOfBounds(
        this IEnumerable<(int rowNumber, int colNumber)> coordinates, int rowCount, int colCount)
    {
        return coordinates.Where(coord => coord is { rowNumber: >= 0, colNumber: >= 0 } && coord.rowNumber < rowCount && coord.colNumber < colCount);
    }

    public static IEnumerable<Number> DistinctNumbersSelectedFromGrid(
        this IEnumerable<(int rowNumber, int colNumber)> coordinates, Schema[,] grid)
    {
        return coordinates
            .Select(coord => grid[coord.rowNumber, coord.colNumber])
            .OfType<Number>()
            .DistinctBy(number => number.Discriminator);
    }

    public static T[,] ToTwoDimensionalArray<T>(this IEnumerable<IEnumerable<T>> twoDimensionalEnumerable)
    {
        var asArray = twoDimensionalEnumerable.Select(inner => inner.ToArray()).ToArray();
        var (rowCount, colCount) = (asArray.Length, asArray.Select(inner => inner.Length).Distinct().SingleOrDefault());
        if (rowCount != colCount) throw new ApplicationException($"Grid is not square: {rowCount}x{colCount}");

        var twoDimensionalArray = new T[rowCount, colCount];

        var (rowNumber, colNumber) = (0, 0);
        while (rowNumber < rowCount)
        {
            twoDimensionalArray[rowNumber, colNumber] = asArray[rowNumber][colNumber];

            colNumber++;
            if (colNumber == colCount)
            {
                rowNumber++;
                colNumber = 0;
            }
        }

        return twoDimensionalArray;
    }

    public static IEnumerable<IEnumerable<T>> AsEnumerable<T>(this T[,] twoDimensionalArray)
    {
        return AsEnumerable(twoDimensionalArray).Chunk(twoDimensionalArray.GetLength(1));

        static IEnumerable<T> AsEnumerable(T[,] twoDimensionalArray)
        {
            foreach (var item in twoDimensionalArray)
                yield return item;
        }
    }
}

internal abstract record Schema
{
    public static Schema CreatePeriod() => new Period();
    public static Schema CreateSymbol(char symbol) => new Symbol(symbol);
    public static Schema CreateNumber(char digit) => new Number(int.Parse(digit.ToString()));

    public Guid Discriminator = Guid.NewGuid();
}
internal record Period : Schema;
internal record Symbol(char Value) : Schema;
internal record Number(int Value) : Schema
{
    public static Number Merge(IEnumerable<Number> numbers)
    {
        var number = numbers.Aggregate("", (seed, n) => seed += n.Value);
        return new(int.Parse(number));
    }
};
