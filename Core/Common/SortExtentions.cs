namespace Core.Common;

using System.Collections.Generic;
using System.Linq;

public static class SortExtensions
{
    // Сортировка с поддержкой нескольких SortDescriptor
    public static IOrderedEnumerable<T> ApplySort<T>(this IEnumerable<T> source, SortDescriptor<T> first, params SortDescriptor<T>[] others)
    {
        IOrderedEnumerable<T> result = first.Direction == SortDirection.Ascending
            ? source.OrderBy(first.KeySelector.Compile())
            : source.OrderByDescending(first.KeySelector.Compile());

        foreach (var descriptor in others)
        {
            result = descriptor.Direction == SortDirection.Ascending
                ? result.ThenBy(descriptor.KeySelector.Compile())
                : result.ThenByDescending(descriptor.KeySelector.Compile());
        }

        return result;
    }
}
