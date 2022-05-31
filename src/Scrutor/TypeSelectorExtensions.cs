using System;
using JetBrains.Annotations;

namespace Scrutor;

[PublicAPI]
public static class TypeSelectorExtensions
{
    [Obsolete("This method has been marked obsolete and will be removed in the next major version. Use " + nameof(FromTypes) + " instead.")]
    public static IServiceTypeSelector AddType<T>(this ITypeSelector selector) => selector.FromType<T>();

    public static IServiceTypeSelector FromType<T>(this ITypeSelector selector)
    {
        Preconditions.NotNull(selector, nameof(selector));

        return selector.FromTypes(typeof(T));
    }

    [Obsolete("This method has been marked obsolete and will be removed in the next major version. Use " + nameof(FromTypes) + " instead.")]
    public static IServiceTypeSelector AddTypes<T1, T2>(this ITypeSelector selector) => selector.FromTypes<T1, T2>();

    public static IServiceTypeSelector FromTypes<T1, T2>(this ITypeSelector selector)
    {
        Preconditions.NotNull(selector, nameof(selector));

        return selector.FromTypes(typeof(T1), typeof(T2));
    }

    [Obsolete("This method has been marked obsolete and will be removed in the next major version. Use " + nameof(FromTypes) + " instead.")]
    public static IServiceTypeSelector AddTypes<T1, T2, T3>(this ITypeSelector selector) => selector.FromTypes<T1, T2, T3>();

    public static IServiceTypeSelector FromTypes<T1, T2, T3>(this ITypeSelector selector)
    {
        Preconditions.NotNull(selector, nameof(selector));

        return selector.FromTypes(typeof(T1), typeof(T2), typeof(T3));
    }
}
