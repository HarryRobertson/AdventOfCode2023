using System;
using System.Collections.Generic;
using System.Linq;

namespace AdventOfCode;

internal static class UtilityExensions
{
    public static PrintableCollection<T> ToPrintableCollection<T>(this IEnumerable<T> values) => new(values.ToList());
    
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