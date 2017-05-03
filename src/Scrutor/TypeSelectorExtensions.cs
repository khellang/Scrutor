namespace Scrutor
{
    public static class TypeSelectorExtensions
    {
        public static IServiceTypeSelector AddType<T>(this ITypeSelector selector)
        {
            Preconditions.NotNull(selector, nameof(selector));

            return selector.AddTypes(typeof(T));
        }

        public static IServiceTypeSelector AddTypes<T1, T2>(this ITypeSelector selector)
        {
            Preconditions.NotNull(selector, nameof(selector));

            return selector.AddTypes(typeof(T1), typeof(T2));
        }

        public static IServiceTypeSelector AddTypes<T1, T2, T3>(this ITypeSelector selector)
        {
            Preconditions.NotNull(selector, nameof(selector));

            return selector.AddTypes(typeof(T1), typeof(T2), typeof(T3));
        }
    }
}
