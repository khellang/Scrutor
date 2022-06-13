using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Scrutor;

public enum SkipBehavior
{
    /// <summary>
    /// Skip registration if the service type has already been registered. Same as <see cref="ServiceCollectionDescriptorExtensions.TryAdd(IServiceCollection, Microsoft.Extensions.DependencyInjection.ServiceDescriptor)"/>.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Skip registration if the service type has already been registered (default). Same as <see cref="ServiceCollectionDescriptorExtensions.TryAdd(IServiceCollection, Microsoft.Extensions.DependencyInjection.ServiceDescriptor)"/>.
    /// </summary>
    ServiceType = 1,

    /// <summary>
    /// Skip registration if the implementation type has already been registered.
    /// </summary>
    ImplementationType = 2,

    /// <summary>
    /// Skip registration if a descriptor with the same <see cref="ServiceDescriptor.ServiceType"/> and implementation has already been registered.
    /// </summary>
    Exact = 3,
}

