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

    public abstract Func<IServiceProvider, object> CreateDecorator(ServiceDescriptor descriptor);

    internal static DecorationStrategy WithType(Type serviceType, Type decoratorType) =>
        Create(serviceType, decoratorType, decoratorFactory: null);

    internal static DecorationStrategy WithFactory(Type serviceType, Func<object, IServiceProvider, object> decoratorFactory) =>
        Create(serviceType, decoratorType: null, decoratorFactory);

    private protected static Func<IServiceProvider, object> TypeDecorator(ServiceDescriptor descriptor, Type decoratorType) => serviceProvider =>
    {
        var instanceToDecorate = GetInstance(serviceProvider, descriptor);
        return ActivatorUtilities.CreateInstance(serviceProvider, decoratorType, instanceToDecorate);
    };

    private protected static Func<IServiceProvider, object> FactoryDecorator(ServiceDescriptor descriptor, Func<object, IServiceProvider, object> decoratorFactory) => serviceProvider =>
    {
        var instanceToDecorate = GetInstance(serviceProvider, descriptor);
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

    private static object GetInstance(IServiceProvider provider, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance != null)
        {
            return descriptor.ImplementationInstance;
        }

        var implementationType = descriptor.ImplementationType;
        if (implementationType != null)
        {
            return ActivatorUtilities.CreateInstance(provider, implementationType);
        }

        if (descriptor.ImplementationFactory != null)
        {
            return descriptor.ImplementationFactory(provider);
        }

        throw new InvalidOperationException($"No implementation factory or instance or type found for {descriptor.ServiceType}.");
    }
}
