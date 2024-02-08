#if !NET8_0_OR_GREATER

using System.Collections.Generic;

namespace Scrutor;

internal static class EnumerableExtensions
{
    public static ISet<T> ToHashSet<T>(this IEnumerable<T> source)
    {
        if (source is ISet<T> set)
        {
            return set;
        }

        return new HashSet<T>(source);
    }
}

#endif
