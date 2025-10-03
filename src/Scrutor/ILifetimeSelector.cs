using Microsoft.Extensions.DependencyInjection;
using System;

namespace Scrutor;

public interface ILifetimeSelector : IServiceTypeSelector
{
    /// <summary>
    /// Registers each matching concrete type with <see cref="ServiceLifetime.Singleton"/> lifetime.
    /// </summary>
    IImplementationTypeSelector WithSingletonLifetime();

    /// <summary>
    /// Registers each matching concrete type with <see cref="ServiceLifetime.Scoped"/> lifetime.
    /// </summary>
    IImplementationTypeSelector WithScopedLifetime();

    /// <summary>
    /// Registers each matching concrete type with <see cref="ServiceLifetime.Transient"/> lifetime.
    /// </summary>
    IImplementationTypeSelector WithTransientLifetime();

    /// <summary>
    /// Registers each matching concrete type with the specified <paramref name="lifetime"/>.
    /// </summary>
    IImplementationTypeSelector WithLifetime(ServiceLifetime lifetime);

    /// <summary>
    /// Registers each matching concrete type with a lifetime based on the provided <paramref name="selector"/>.
    /// </summary>
    IImplementationTypeSelector WithLifetime(Func<Type, ServiceLifetime> selector);

    /// <summary>
    /// Registers each matching concrete type with the specified <paramref name="serviceKey"/>.
    /// </summary>
    /// <param name="serviceKey">The service key to use for registration.</param>
    ILifetimeSelector WithServiceKey(object serviceKey);

    /// <summary>
    /// Registers each matching concrete type with a service key based on the provided <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">A function to determine the service key for each type.</param>
    ILifetimeSelector WithServiceKey(Func<Type, object?> selector);
}
