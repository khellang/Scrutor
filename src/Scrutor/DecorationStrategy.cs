using System;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor;

public abstract class DecorationStrategy
{
    protected DecorationStrategy(Type serviceType)
    {
        ServiceType = serviceType;
    }

    public Type ServiceType { get; }

    public abstract bool CanDecorate(Type serviceType);

    public abstract Func<IServiceProvider, object?, object> CreateDecorator(Type serviceType, string serviceKey);

    internal static DecorationStrategy WithType(Type serviceType, Type decoratorType) =>
        Create(serviceType, decoratorType, decoratorFactory: null);

    internal static DecorationStrategy WithFactory(Type serviceType, Func<object, IServiceProvider, object> decoratorFactory) =>
        Create(serviceType, decoratorType: null, decoratorFactory);

    protected static Func<IServiceProvider, object?, object> TypeDecorator(Type serviceType, string serviceKey, Type decoratorType)
    {
        var factory = ActivatorUtilities.CreateFactory(decoratorType, new[] { serviceType });
        return (serviceProvider, _) =>
        {
            var instanceToDecorate = serviceProvider.GetRequiredKeyedService(serviceType, serviceKey);
            return factory(serviceProvider, new object[] { instanceToDecorate });
        };
    }

    protected static Func<IServiceProvider, object?, object> FactoryDecorator(Type serviceType, string serviceKey, Func<object, IServiceProvider, object> decoratorFactory) => (serviceProvider, _) =>
    {
        var instanceToDecorate = serviceProvider.GetRequiredKeyedService(serviceType, serviceKey);
        return decoratorFactory(instanceToDecorate, serviceProvider);
    };

    private static DecorationStrategy Create(Type serviceType, Type? decoratorType, Func<object, IServiceProvider, object>? decoratorFactory)
    {
        if (serviceType.IsOpenGeneric())
        {
            return new OpenGenericDecorationStrategy(serviceType, decoratorType, decoratorFactory);
        }

        return new ClosedTypeDecorationStrategy(serviceType, decoratorType, decoratorFactory);
    }
}
