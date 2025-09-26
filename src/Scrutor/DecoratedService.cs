using System;
using System.Collections.Generic;

namespace Scrutor;

/// <summary>
/// A handle to a decorated service. This can be used to resolve decorated services from an <see cref="IServiceProvider"/>
/// using <see cref="ServiceProviderExtensions.GetRequiredDecoratedService{TService}(IServiceProvider, DecoratedService{TService})"/>.
/// </summary>
/// <typeparam name="TService">The type of services which were decorated.</typeparam>
public sealed class DecoratedService<TService>
{
    internal DecoratedService(Type serviceType, IReadOnlyList<string> serviceKeys)
    {
        if (!typeof(TService).IsAssignableFrom(serviceType))
            throw new ArgumentException($"The type {serviceType} is not assignable to the service type {typeof(TService)}");

        ServiceType = serviceType;
        ServiceKeys = serviceKeys;
    }

    internal Type ServiceType { get; }
    internal IReadOnlyList<string> ServiceKeys { get; } // In descending order of precedence

    internal DecoratedService<TService2> Downcast<TService2>() => new(ServiceType, ServiceKeys);
}
