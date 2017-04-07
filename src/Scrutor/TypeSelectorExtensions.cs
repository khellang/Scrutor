namespace Scrutor
{
    public static class TypeSelectorExtensions
    {
        public static IServiceTypeSelector These<T>(this ITypeSelector selector)
        {
            return selector.These(typeof(T));
        }
        public static IServiceTypeSelector These<T1, T2>(this ITypeSelector selector)
        {
            return selector.These(typeof(T1), typeof(T2));
        }
        public static IServiceTypeSelector These<T1,T2,T3>(this ITypeSelector selector)
        {
            return selector.These(typeof(T1), typeof(T2), typeof(T3));
        }
    }
}