using JetBrains.Annotations;

namespace Scrutor;

[PublicAPI]
public static class TypeSelectorExtensions
{
    public static IServiceTypeSelector FromType<T>(this ITypeSelector selector)
    {
        Preconditions.NotNull(selector, nameof(selector));

        return selector.FromTypes(typeof(T));
    }

    public static IServiceTypeSelector FromTypes<T1, T2>(this ITypeSelector selector)
    {
        Preconditions.NotNull(selector, nameof(selector));

        return selector.FromTypes(typeof(T1), typeof(T2));
    }

    public static IServiceTypeSelector FromTypes<T1, T2, T3>(this ITypeSelector selector)
    {
        Preconditions.NotNull(selector, nameof(selector));

        return selector.FromTypes(typeof(T1), typeof(T2), typeof(T3));
    }
}
