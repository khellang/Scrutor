using System;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor;

internal static class ServiceDescriptorExtensions
{
    public static ServiceDescriptor WithImplementationFactory(this ServiceDescriptor descriptor, Func<IServiceProvider, object> implementationFactory) =>
        new(descriptor.ServiceType, implementationFactory, descriptor.Lifetime);

    public static ServiceDescriptor WithServiceType(this ServiceDescriptor descriptor, Type serviceType) => descriptor switch
    {
        { ImplementationType: not null } => new ServiceDescriptor(serviceType, descriptor.ImplementationType, descriptor.Lifetime),
        { ImplementationFactory: not null } => new ServiceDescriptor(serviceType, descriptor.ImplementationFactory, descriptor.Lifetime),
        { ImplementationInstance: not null } => new ServiceDescriptor(serviceType, descriptor.ImplementationInstance),
        _ => throw new ArgumentException($"No implementation factory or instance or type found for {descriptor.ServiceType}.", nameof(descriptor))
    };
}
