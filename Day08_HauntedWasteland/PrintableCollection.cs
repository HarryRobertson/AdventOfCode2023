using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdventOfCode;

internal class PrintableCollection<T>(ICollection<T> values) : ICollection<T>
{
    public override string ToString()
    {
        return values
            .Aggregate(new StringBuilder("[ "), (builder, value) => builder.Append(value).Append(", "))
            .Append(']')
            .ToString();
    }
    
    public int Count => values.Count;
    public bool IsReadOnly => values.IsReadOnly;
    public void Add(T item) => values.Add(item);
    public void Clear() => values.Clear();
    public bool Contains(T item) => values.Contains(item);
    public void CopyTo(T[] array, int arrayIndex) => values.CopyTo(array, arrayIndex);
    public bool Remove(T item) => values.Remove(item);
    public IEnumerator<T> GetEnumerator() => values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}