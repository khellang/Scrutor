using System;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor;

internal static class ServiceDescriptorExtensions
{
    public static ServiceDescriptor WithImplementationFactory(this ServiceDescriptor descriptor, Func<IServiceProvider, object?, object> implementationFactory) =>
        new(descriptor.ServiceType, descriptor.ServiceKey, implementationFactory, descriptor.Lifetime);

    public static ServiceDescriptor WithServiceKey(this ServiceDescriptor descriptor, string serviceKey) =>
        descriptor.IsKeyedService ? ReplaceServiceKey(descriptor, serviceKey) : AddServiceKey(descriptor, serviceKey);

    private static ServiceDescriptor ReplaceServiceKey(ServiceDescriptor descriptor, string serviceKey) => descriptor switch
    {
        { KeyedImplementationType: not null } => new ServiceDescriptor(descriptor.ServiceType, serviceKey, descriptor.KeyedImplementationType, descriptor.Lifetime),
        { KeyedImplementationFactory: not null } => new ServiceDescriptor(descriptor.ServiceType, serviceKey, descriptor.KeyedImplementationFactory, descriptor.Lifetime),
        { KeyedImplementationInstance: not null } => new ServiceDescriptor(descriptor.ServiceType, serviceKey, descriptor.KeyedImplementationInstance),
        _ => throw new ArgumentException($"No implementation factory or instance or type found for {descriptor.ServiceType}.", nameof(descriptor))
    };

    private static ServiceDescriptor AddServiceKey(ServiceDescriptor descriptor, string serviceKey) => descriptor switch
    {
        { ImplementationType: not null } => new ServiceDescriptor(descriptor.ServiceType, serviceKey, descriptor.ImplementationType, descriptor.Lifetime),
        { ImplementationFactory: not null } => new ServiceDescriptor(descriptor.ServiceType, serviceKey, DiscardServiceKey(descriptor.ImplementationFactory), descriptor.Lifetime),
        { ImplementationInstance: not null } => new ServiceDescriptor(descriptor.ServiceType, serviceKey, descriptor.ImplementationInstance),
        _ => throw new ArgumentException($"No implementation factory or instance or type found for {descriptor.ServiceType}.", nameof(descriptor))
    };

    private static Func<IServiceProvider, object?, object> DiscardServiceKey(Func<IServiceProvider, object> factory) => (sp, key) => factory(sp);
}
