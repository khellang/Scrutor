using System;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor;

internal static class ServiceDescriptorExtensions
{
    public static ServiceDescriptor WithImplementationFactory(this ServiceDescriptor descriptor, Func<IServiceProvider, object?, object> implementationFactory) =>
        new(descriptor.ServiceType, descriptor.ServiceKey, implementationFactory, descriptor.Lifetime);

    public static ServiceDescriptor WithServiceKey(this ServiceDescriptor descriptor, string serviceKey)
    {
        if (descriptor.IsKeyedService)
        {
            if (descriptor.KeyedImplementationType is not null)
            {
                return new ServiceDescriptor(descriptor.ServiceType, serviceKey, descriptor.KeyedImplementationType, descriptor.Lifetime);
            }

            if (descriptor.KeyedImplementationInstance is not null)
            {
                return new ServiceDescriptor(descriptor.ServiceType, serviceKey, descriptor.KeyedImplementationInstance);
            }

            if (descriptor.KeyedImplementationFactory is not null)
            {
                return new ServiceDescriptor(descriptor.ServiceType, serviceKey, descriptor.KeyedImplementationFactory, descriptor.Lifetime);
            }

            throw new InvalidOperationException($"One of the following properties must be set: {nameof(ServiceDescriptor.KeyedImplementationType)}, {nameof(ServiceDescriptor.KeyedImplementationInstance)} or {nameof(ServiceDescriptor.KeyedImplementationFactory)}");
        }

        if (descriptor.ImplementationType is not null)
        {
            return new ServiceDescriptor(descriptor.ServiceType, serviceKey, descriptor.ImplementationType, descriptor.Lifetime);
        }

        if (descriptor.ImplementationInstance is not null)
        {
            return new ServiceDescriptor(descriptor.ServiceType, serviceKey, descriptor.ImplementationInstance);
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return new ServiceDescriptor(descriptor.ServiceType, serviceKey, (sp, key) => descriptor.ImplementationFactory(sp), descriptor.Lifetime);
        }

        throw new InvalidOperationException($"One of the following properties must be set: {nameof(ServiceDescriptor.ImplementationType)}, {nameof(ServiceDescriptor.ImplementationInstance)} or {nameof(ServiceDescriptor.ImplementationFactory)}");
    }
}
