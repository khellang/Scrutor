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

    /// <summary>
    /// Gets the service descriptor's implementation type.
    /// </summary>
    /// <param name="descriptor">The service descriptor.</param>
    /// <returns>System.Type?.</returns>
    /// <remarks>
    /// Mostly replicates <see href="https://source.dot.net/#Microsoft.Extensions.DependencyInjection.Abstractions/ServiceDescriptor.cs,a8b66e844e0ff864,references">ServiceDescriptor.GetImplementationType()</see>
    /// </remarks>
    public static Type? GetImplementationType(this ServiceDescriptor descriptor) 
    {
        if (descriptor.ImplementationType != null)
        {
            return descriptor.ImplementationType;
        }
        else if (descriptor.ImplementationInstance != null)
        {
            return descriptor.ImplementationInstance.GetType();
        }
        else if (descriptor.ImplementationFactory != null) 
        {
            Type[]? typeArguments = descriptor.ImplementationFactory.GetType().GenericTypeArguments;
            if (typeArguments[1] == typeof(object))
            {
                return descriptor.ImplementationFactory.Method.ReturnType;
            }
            else
            {
                return typeArguments[1];
            }	
        }
        return null;
    }
}
