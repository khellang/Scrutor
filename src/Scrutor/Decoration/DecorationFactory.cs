using System;

namespace Scrutor.Decoration
{
    internal static class DecorationFactory
    {
        public static Decorator Create(Type serviceType, Type? decoratorType, Func<object, IServiceProvider, object>? decoratorFactory)
        {
            IDecorationStrategy strategy;

            if (serviceType.IsOpenGeneric())
            {
                strategy = new OpenGenericDecorationStrategy(serviceType, decoratorType, decoratorFactory);
            }
            else
            {
                strategy = new ClosedTypeDecorationStrategy(serviceType, decoratorType, decoratorFactory);
            }

            return new Decorator(strategy);
        }

        public static Decorator Create<TService>(Type? decoratorType, Func<object, IServiceProvider, object>? decoratorFactory)
            => new(new ClosedTypeDecorationStrategy(typeof(TService), decoratorType, decoratorFactory)); 
    }
}
