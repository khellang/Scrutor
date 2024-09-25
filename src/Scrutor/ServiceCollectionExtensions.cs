using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor;

internal static class ServiceCollectionExtensions
{
    public static bool HasRegistration(this IServiceCollection services, Type serviceType)
    {
        return services.Any(x => x.ServiceType == serviceType);
    }

    /// <summary>
    /// Determines whether the service collection has a descriptor with the same Service and Implementation types.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="descriptor">The service descriptor.</param>
    /// <returns><c>true</c> if the service collection contains the specified service descriptor; otherwise, <c>false</c>.</returns>
    public static bool HasRegistration(this IServiceCollection services, ServiceDescriptor descriptor)
    {
        return services.Any(x => x.ServiceType == descriptor.ServiceType && x.ImplementationType == descriptor.ImplementationType);
    }
}
