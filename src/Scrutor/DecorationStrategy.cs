using System;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor;

public abstract class DecorationStrategy
{
    protected DecorationStrategy(Type serviceType, string? serviceKey)
    {
        ServiceType = serviceType;
        ServiceKey = serviceKey;
    }

    public Type ServiceType { get; }

    public string? ServiceKey { get; }

    public virtual bool CanDecorate(ServiceDescriptor descriptor) =>
        string.Equals(ServiceKey, descriptor.ServiceKey) && CanDecorate(descriptor.ServiceType);

    protected abstract bool CanDecorate(Type serviceType);

    public abstract Func<IServiceProvider, object?, object> CreateDecorator(Type serviceType, string serviceKey);

    internal static DecorationStrategy WithType(Type serviceType, string? serviceKey, Type decoratorType) =>
        Create(serviceType, serviceKey, decoratorType, decoratorFactory: null);

    internal static DecorationStrategy WithFactory(Type serviceType, string? serviceKey, Func<object, IServiceProvider, object> decoratorFactory) =>
        Create(serviceType, serviceKey, decoratorType: null, decoratorFactory);

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

    private static DecorationStrategy Create(Type serviceType, string? serviceKey, Type? decoratorType, Func<object, IServiceProvider, object>? decoratorFactory)
    {
        if (serviceType.IsOpenGeneric())
        {
            return new OpenGenericDecorationStrategy(serviceType, serviceKey, decoratorType, decoratorFactory);
        }

        return new ClosedTypeDecorationStrategy(serviceType, serviceKey, decoratorType, decoratorFactory);
    }
}
